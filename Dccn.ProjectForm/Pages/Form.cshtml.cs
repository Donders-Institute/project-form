using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Pages
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class FormModel : PageModel
    {
        public static readonly IEnumerable<SectionInfo> FormSections = new []
        {
            new SectionInfo(m => m.General),
            new SectionInfo(m => m.Funding),
            new SectionInfo(m => m.Ethics),
            new SectionInfo(m => m.Experiment),
            new SectionInfo(m => m.DataManagement),
            new SectionInfo(m => m.Privacy),
            new SectionInfo(m => m.Payment)
        };

        private readonly IAuthorizationService _authorizationService;
        private readonly ProposalsDbContext _proposalsDbContext;
        private readonly ProjectsDbContext _projectsDbContext;
        private readonly IModalityProvider _modalityProvider;
        private readonly UserManager<ProjectsUser> _userManager;
        private readonly IFormSectionHandlerRegistry _sectionHandlerRegistry;

        public FormModel(IAuthorizationService authorizationService, ProposalsDbContext proposalsDbContext, ProjectsDbContext projectsDbContext, IModalityProvider modalityProvider, UserManager<ProjectsUser> userManager, IFormSectionHandlerRegistry sectionHandlerRegistry)
        {
            _authorizationService = authorizationService;
            _proposalsDbContext = proposalsDbContext;
            _projectsDbContext = projectsDbContext;
            _modalityProvider = modalityProvider;
            _userManager = userManager;
            _sectionHandlerRegistry = sectionHandlerRegistry;
        }

        public General General { get; set; } = new General();
        public Funding Funding { get; set; } = new Funding();
        public Ethics Ethics { get; set; } = new Ethics();
        public Experiment Experiment { get; set; } = new Experiment();
        public DataManagement DataManagement { get; set; } = new DataManagement();
        public Privacy Privacy { get; set; } = new Privacy();
        public Payment Payment { get; set; } = new Payment();

        [UsedImplicitly]
        public async Task<IActionResult> OnGetAsync(int proposalId)
        {
            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            await LoadFormAsync(proposal);
            return Page();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostAsync(
            int proposalId,
            [FromForm(Name = nameof(General))] General general,
            [FromForm(Name = nameof(Funding))] Funding funding,
            [FromForm(Name = nameof(Ethics))] Ethics ethics,
            [FromForm(Name = nameof(Experiment))] Experiment experiment,
            [FromForm(Name = nameof(DataManagement))] DataManagement dataManagement,
            [FromForm(Name = nameof(Privacy))] Privacy privacy,
            [FromForm(Name = nameof(Payment))] Payment payment)
        {
            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            General = general;
            Funding = funding;
            Ethics = ethics;
            Experiment = experiment;
            DataManagement = dataManagement;
            Privacy = privacy;
            Payment = payment;

            await StoreFormAsync(proposal);

            if (!ModelState.IsValid)
            {
                return new JsonResult(new SerializableError(ModelState));
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> OnPostRequestApprovalAsync(int proposalId, [Required][FromQuery(Name = "section")] string sectionId)
        {
            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            var section = FormSections.FirstOrDefault(i => i.Id == sectionId)?.GetModel(this);
            if (section == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            foreach (var approvalInfo in section.ApprovalInfo)
            {
                if (approvalInfo.Status == ApprovalStatus.NotSubmitted ||
                    approvalInfo.Status == ApprovalStatus.Rejected)
                {
                    await _sectionHandlerRegistry.GetHandler(section.GetType()).ValidateExAsync(ModelState);
                }
            }

            return RedirectToPage(null, null, new { proposalId }, sectionId);
        }

        private async Task<(Proposal Proposal, ActionResult Error)> LoadProposalAsync(int proposalId)
        {
            var proposal = await _proposalsDbContext.Proposals
                .Include(p => p.Labs)
                .Include(p => p.Experimenters)
                .Include(p => p.DataAccessRules)
                .Include(p => p.Approvals)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null)
            {
                return (null, NotFound());
            }

            var authorization = await _authorizationService.AuthorizeAsync(User, proposal, FormOperations.Edit);
            if (authorization.Succeeded)
            {
                return (proposal, null);
            }

            if (User.Identity.IsAuthenticated)
            {
                return (null, Forbid());
            }

            return (null, Challenge());
        }

        private async Task LoadFormAsync(Proposal proposal)
        {
            var owner = await _projectsDbContext.Users.FindAsync(proposal.OwnerId);
            var supervisor = await _projectsDbContext.Users.FindAsync(proposal.SupervisorId);

            foreach (var sectionInfo in FormSections)
            {
                await _sectionHandlerRegistry.LoadSectionAsync(sectionInfo.GetModel(this), proposal, owner, supervisor);
            }
        }

        private async Task StoreFormAsync(Proposal proposal)
        {
            _proposalsDbContext.Proposals.Attach(proposal);

            foreach (var sectionInfo in FormSections)
            {
                var sectionValid = true;

                if (!ModelState.IsValid)
                {
                    sectionValid = ModelState
                        .FindKeysWithPrefix(sectionInfo.Id)
                        .Select(entry => entry.Value)
                        .All(state => state.ValidationState == ModelValidationState.Valid);
                }

                var model = sectionInfo.GetModel(this);
                if (sectionValid && model != null)
                {
                    await _sectionHandlerRegistry.StoreSectionAsync(model, proposal);
                }
            }

            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);

            await _proposalsDbContext.SaveChangesAsync();
        }
    }
}