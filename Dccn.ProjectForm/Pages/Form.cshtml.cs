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
        public SubmissionSectionModel Submission { get; } = new SubmissionSectionModel();

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

        [UsedImplicitly]
        public async Task<IActionResult> OnPostSubmitAsync(int proposalId, [Required] string sectionId)
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

            var sectionHandler = _sectionHandlers.Single(h => h.Id == sectionId);
            if (!await AuthorizeAsync(proposal, FormSectionOperation.Submit(sectionHandler.ModelType)))
            {
                return Forbid();
            }

            if (!await sectionHandler.ValidateProposalAsync(this, proposal))
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            foreach (var approval in sectionHandler.GetAssociatedApprovals(proposal))
            {
                if (sectionHandler.IsAuthorityApplicable(proposal, approval.AuthorityRole))
                {
                    var authority = await _authorityProvider.GetAuthorityAsync(proposal, approval.AuthorityRole);
                    // Is dummy authority (no approval required)?
                    if (authority == null)
                    {
                        approval.Status = ApprovalStatus.Approved;
                    }
                    else
                    {
                        await _emailService.SendEmailAsync(User, new ApprovalRequestModel
                        {
                            Recipient = new MailAddress(authority.Email, authority.DisplayName),

                            ApproverName = authority.DisplayName,
                            ApplicantName = _userManager.GetUserName(User),
                            ProposalTitle = proposal.Title,
                            SectionName = sectionHandler.DisplayName,
                            PageLink = Url.Page(null, null, new { proposalId }, "https", null, sectionHandler.Id)
                        });
                        approval.Status = ApprovalStatus.ApprovalPending;
                    }
                }
                else
                {
                    approval.Status = ApprovalStatus.NotApplicable;
                }
            }

            await _proposalsDbContext.SaveChangesAsync();

            return RedirectToPage(null, null, new { proposalId }, sectionId);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostRetractAsync(int proposalId, [Required] string sectionId)
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

            var sectionHandler = _sectionHandlers.Single(h => h.Id == sectionId);
            if (!await AuthorizeAsync(proposal, FormSectionOperation.Retract(sectionHandler.ModelType)))
            {
                return Forbid();
            }

            foreach (var approval in sectionHandler.GetAssociatedApprovals(proposal))
            {
                approval.Status = ApprovalStatus.NotSubmitted;
                approval.ValidatedBy = null;
            }

            await _proposalsDbContext.SaveChangesAsync();

            return RedirectToPage(null, null, new { proposalId }, sectionId);
        }

        [UsedImplicitly]
        public Task<IActionResult> OnPostApproveAsync(int proposalId, [Required] string sectionId)
        {
            return ApproveOrRejectAsync(proposalId, sectionId, true);
        }

        [UsedImplicitly]
        public Task<IActionResult> OnPostRejectAsync(int proposalId, [Required] string sectionId)
        {
            return ApproveOrRejectAsync(proposalId, sectionId, false);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostAddLabAsync(int proposalId, [Required] string modality)
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

            if (!await AuthorizeAsync(proposal, FormSectionOperation.Edit<ExperimentSectionModel>()))
            {
                return Forbid();
            }

            var lab = new Lab
            {
                Modality = modality
            };

            proposal.Labs.Add(lab);
            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);

            await _proposalsDbContext.SaveChangesAsync();

            return new JsonResult(new
            {
                LabId = lab.Id,
                proposal.Timestamp
            });
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostRemoveLabAsync(int proposalId, [Required] int labId)
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

            if (!await AuthorizeAsync(proposal, FormSectionOperation.Edit<ExperimentSectionModel>()))
            {
                return Forbid();
            }

            var lab = proposal.Labs.FirstOrDefault(e => e.Id == labId);
            if (lab == null)
            {
                ModelState.AddModelError(nameof(labId), "Lab with the given ID does not exist.");
                return BadRequest(ModelState);
            }

            proposal.Labs.Remove(lab);
            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);

            await _proposalsDbContext.SaveChangesAsync();

            return new JsonResult(new
            {
                proposal.Timestamp
            });
        }

        private async Task<IActionResult> ApproveOrRejectAsync(int proposalId, string sectionId, bool approve)
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

            var sectionHandler = _sectionHandlers.Single(h => h.Id == sectionId);
            var operation = approve
                ? FormSectionOperation.Approve(sectionHandler.ModelType)
                : FormSectionOperation.Reject(sectionHandler.ModelType);
            if (!await AuthorizeAsync(proposal, operation))
            {
                return Forbid();
            }

            if (!await sectionHandler.ValidateProposalAsync(this, proposal))
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            var approval = sectionHandler
                .GetAssociatedApprovals(proposal)
                .Single(a => _userManager.IsInApprovalRole(User, a.AuthorityRole)
                             || a.AuthorityRole == ApprovalAuthorityRole.Supervisor
                             && proposal.SupervisorId == _userManager.GetUserId(User));

            approval.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            approval.ValidatedBy = _userManager.GetUserId(User);

            await _proposalsDbContext.SaveChangesAsync();

            return RedirectToPage(null, null, new { proposalId }, sectionId);
        }

        private async Task<(Proposal Proposal, IActionResult Error)> LoadProposalAsync(int proposalId)
        {
            var proposal = await _proposalsDbContext.Proposals
                .Include(p => p.Labs)
                .Include(p => p.Experimenters)
                .Include(p => p.StorageAccessRules)
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
                model.CanEdit = await AuthorizeAsync(proposal, FormSectionOperation.Edit(sectionHandler.ModelType));
                model.CanSubmit = await AuthorizeAsync(proposal, FormSectionOperation.Submit(sectionHandler.ModelType));
                model.CanRetract = await AuthorizeAsync(proposal, FormSectionOperation.Retract(sectionHandler.ModelType));
                model.CanApprove = await AuthorizeAsync(proposal, FormSectionOperation.Approve(sectionHandler.ModelType));
                model.CanReject = await AuthorizeAsync(proposal, FormSectionOperation.Reject(sectionHandler.ModelType));
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

                if (!await AuthorizeAsync(proposal, FormSectionOperation.Edit(sectionHandler.ModelType)))
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