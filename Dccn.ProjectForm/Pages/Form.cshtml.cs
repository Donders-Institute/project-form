using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Email.Models;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Pages
{
    public class FormModel : PageModel
    {
        private readonly ProposalsDbContext _proposalsDbContext;
        private readonly IUserManager _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEmailService _emailService;
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;
        private readonly IAuthorityProvider _authorityProvider;

        public FormModel(ProposalsDbContext proposalsDbContext, IUserManager userManager, IAuthorizationService authorizationService, IEmailService emailService, IEnumerable<IFormSectionHandler> sectionHandlers, IAuthorityProvider authorityProvider)
        {
            _proposalsDbContext = proposalsDbContext;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _emailService = emailService;
            _sectionHandlers = sectionHandlers;
            _authorityProvider = authorityProvider;
        }

        public GeneralSectionModel General { get; } = new GeneralSectionModel();
        public FundingSectionModel Funding { get; } = new FundingSectionModel();
        public EthicsSectionModel Ethics { get; } = new EthicsSectionModel();
        public ExperimentSectionModel Experiment { get; } = new ExperimentSectionModel();
        public DataSectionModel Data { get; } = new DataSectionModel();
        public PrivacySectionModel Privacy { get; } = new PrivacySectionModel();
        public PaymentSectionModel Payment { get; } = new PaymentSectionModel();

        public ICollection<ISectionModel> Sections { get; private set; }

        [BindProperty]
        [Required]
        public byte[] Timestamp { get; private set; }

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
        public async Task<IActionResult> OnPostAsync(int proposalId, string sectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            error = await StoreFormAsync(proposal, sectionId);
            if (error != null)
            {
                return error;
            }

            return new JsonResult(new
            {
                proposal.Timestamp,
                Errors = new SerializableError(ModelState)
            });
        }

        public async Task<IActionResult> OnPostRequestApprovalAsync(int proposalId, [Required] string sectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            var sectionHandler = _sectionHandlers.Single(h => h.Id == sectionId);
            if (!await AuthorizeAsync(proposal, FormSectionOperation.Submit(sectionHandler)))
            {
                return Forbid();
            }

            foreach (var approval in sectionHandler.GetAssociatedApprovals(proposal))
            {
                if (sectionHandler.IsAuthorityApplicable(proposal, approval.AuthorityRole))
                {
                    var authority = await _authorityProvider.GetAuthorityAsync(proposal, approval.AuthorityRole);
                    await _emailService.SendEmailAsync(User, new ApprovalRequest
                    {
                        Applicant = _userManager.GetUserName(User),
                        ProposalTitle = proposal.Title,
                        SectionName = sectionHandler.DisplayName,
                        Recipient = new MailAddress(authority.Email, authority.DisplayName),
                    });
                    approval.Status = ApprovalStatus.ApprovalPending;
                }
                else
                {
                    approval.Status = ApprovalStatus.NotApplicable;
                }
            }

            await _proposalsDbContext.SaveChangesAsync();

            return RedirectToPage(null, null, new { proposalId }, sectionId);
        }

        private async Task<(Proposal Proposal, IActionResult Error)> LoadProposalAsync(int proposalId)
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

            if (!User.Identity.IsAuthenticated)
            {
                return (null, Challenge());
            }

            if (!await AuthorizeAsync(proposal, FormOperation.View))
            {
                return (null, Forbid());
            }

            if (Timestamp != null && !proposal.Timestamp.SequenceEqual(Timestamp))
            {
                return (null, new ConflictResult());
            }

            Timestamp = proposal.Timestamp;

            return (proposal, null);
        }

        private async Task LoadFormAsync(Proposal proposal)
        {
            Sections = _sectionHandlers.Select(h => h.GetModel(this)).ToList();

            foreach (var sectionHandler in _sectionHandlers)
            {
                await sectionHandler.LoadAsync(this, proposal);

                var model = sectionHandler.GetModel(this);
                model.CanEdit = await AuthorizeAsync(proposal, FormSectionOperation.Edit(sectionHandler));
                model.CanApprove = await AuthorizeAsync(proposal, FormSectionOperation.Approve(sectionHandler));
                model.CanSubmit = await AuthorizeAsync(proposal, FormSectionOperation.Submit(sectionHandler));
            }
        }

        private async Task<IActionResult> StoreFormAsync(Proposal proposal, string sectionId = null)
        {
            foreach (var sectionHandler in _sectionHandlers)
            {
                if (!string.IsNullOrEmpty(sectionId) && sectionId != sectionHandler.Id)
                {
                    continue;
                }

                if (!await AuthorizeAsync(proposal, FormSectionOperation.Edit(sectionHandler)))
                {
                    return Forbid();
                }

                if (!await TryUpdateModelAsync(sectionHandler.GetModel(this), sectionHandler.ModelType, sectionHandler.Id))
                {
                    continue;
                }

                await sectionHandler.StoreAsync(this, proposal);
            }

            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);

            await _proposalsDbContext.SaveChangesAsync();

            return null;
        }

        private async Task<bool> AuthorizeAsync(Proposal proposal, IAuthorizationRequirement requirement)
        {
            return (await _authorizationService.AuthorizeAsync(User, proposal, requirement)).Succeeded;
        }
    }
}