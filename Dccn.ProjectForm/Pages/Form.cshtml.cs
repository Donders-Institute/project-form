using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly IAuthorizationService _authorizationService;
        private readonly ProposalsDbContext _proposalsDbContext;
        private readonly ProjectsDbContext _projectsDbContext;
        private readonly IModalityProvider _modalityProvider;
        private readonly UserManager<ProjectsUser> _userManager;
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;

        public FormModel(IAuthorizationService authorizationService, ProposalsDbContext proposalsDbContext, ProjectsDbContext projectsDbContext, IModalityProvider modalityProvider, UserManager<ProjectsUser> userManager, IEnumerable<IFormSectionHandler> sectionHandlers)
        {
            _authorizationService = authorizationService;
            _proposalsDbContext = proposalsDbContext;
            _projectsDbContext = projectsDbContext;
            _modalityProvider = modalityProvider;
            _userManager = userManager;
            _sectionHandlers = sectionHandlers;
        }

        public General General { get; private set; } = new General();
        public Funding Funding { get; private set; } = new Funding();
        public Ethics Ethics { get; private set; } = new Ethics();
        public Experiment Experiment { get; private set; } = new Experiment();
        public DataManagement DataManagement { get; private set; } = new DataManagement();
        public Privacy Privacy { get; private set; } = new Privacy();
        public Payment Payment { get; private set; } = new Payment();

        public ICollection<SectionInfo> SectionInfo { get; private set; }

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

            if (!ModelState.IsValid)
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            var sectionHandler = _sectionHandlers.Single(h => h.ModelId == sectionId);
            if (sectionHandler.RequestApproval(proposal))
            {
                await _proposalsDbContext.SaveChangesAsync();
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

            SectionInfo = _sectionHandlers.Select(h => new SectionInfo
            {
                Model = h.GetModel(this),
                Expression = h.ModelExpression,
                Id = h.ModelId
            }).ToList();

            foreach (var sectionHandler in _sectionHandlers)
            {
                await sectionHandler.LoadAsync(this, proposal, owner, supervisor);
            }
        }

        private async Task StoreFormAsync(Proposal proposal)
        {
            _proposalsDbContext.Proposals.Attach(proposal);

            foreach (var sectionHandler in _sectionHandlers)
            {
                var sectionValid = true;

                if (!ModelState.IsValid)
                {
                    sectionValid = ModelState
                        .FindKeysWithPrefix(sectionHandler.ModelId)
                        .Select(entry => entry.Value)
                        .All(state => state.ValidationState == ModelValidationState.Valid);
                }

                if (sectionValid)
                {
                    await sectionHandler.StoreAsync(this, proposal);
                }
            }

            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);

            await _proposalsDbContext.SaveChangesAsync();
        }
    }
}