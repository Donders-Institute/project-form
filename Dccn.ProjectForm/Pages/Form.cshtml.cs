using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.Email.Models;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm.Pages
{
    public class FormModel : PageModel
    {
        private readonly ProposalDbContext _proposalDbContext;
        private readonly IUserManager _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEmailService _emailService;
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;
        private readonly IAuthorityProvider _authorityProvider;
        private readonly ILogger _logger;

        public FormModel(
            ProposalDbContext proposalDbContext,
            IUserManager userManager,
            IAuthorizationService authorizationService,
            IEmailService emailService,
            IEnumerable<IFormSectionHandler> sectionHandlers,
            IAuthorityProvider authorityProvider,
            ILogger<FormModel> logger
        ) {
            _proposalDbContext = proposalDbContext;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _emailService = emailService;
            _sectionHandlers = sectionHandlers;
            _authorityProvider = authorityProvider;
            _logger = logger;
        }

        public GeneralSectionModel General { get; } = new GeneralSectionModel();
        public FundingSectionModel Funding { get; } = new FundingSectionModel();
        public EthicsSectionModel Ethics { get; } = new EthicsSectionModel();
        public ExperimentSectionModel Experiment { get; } = new ExperimentSectionModel();
        public DataSectionModel Data { get; } = new DataSectionModel();
        public PrivacySectionModel Privacy { get; } = new PrivacySectionModel();
        public PaymentSectionModel Payment { get; } = new PaymentSectionModel();
        public SubmissionSectionModel Submission { get; } = new SubmissionSectionModel();

        public ApproveSectionModel ApproveSection { get; } = new ApproveSectionModel();
        public RejectSectionModel RejectSection { get; } = new RejectSectionModel();
        public SubmitSectionModel SubmitSection { get; } = new SubmitSectionModel();
        public RetractSectionModel RetractSection { get; } = new RetractSectionModel();

        public ICollection<ISectionModel> Sections { get; private set; }
        public int ProposalId { get; private set; }

        [BindProperty]
        [BindRequired]
        [Required]
        public byte[] Timestamp { get; set; }

        [UsedImplicitly]
        public async Task<IActionResult> OnGetAsync(int proposalId, string sectionId = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            switch (sectionId)
            {
                case null:
                    await LoadFormAsync(proposal);
                    return Page();
                case "Nav":
                    await LoadFormAsync(proposal);
                    return Partial("Shared/_SectionNav", this);
                default:
                {
                    var sectionHandler = _sectionHandlers.SingleOrDefault(h => h.Id == sectionId);
                    if (sectionHandler == null)
                    {
                        return NotFound();
                    }

                    await LoadSectionAsync(proposal, sectionHandler);

                    ViewData["Section"] = sectionHandler.GetModel(this);
                    return Partial("Shared/_Section", this);
                }
            }
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostAsync(int proposalId, [Required] string sectionId)
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
            if (!await AuthorizeAsync(proposal, FormSectionOperation.Edit(sectionHandler.ModelType)))
            {
                return Forbid();
            }

            if (await TryUpdateModelAsync(sectionHandler.GetModel(this), sectionHandler.ModelType, sectionHandler.Id))
            {
                await sectionHandler.StoreAsync(this, proposal);
                await UpdateProposalAsync(proposal);
            }

            return new JsonResult(new
            {
                proposal.Timestamp,
                Success = ModelState.IsValid,
                Errors = new SerializableError(ModelState)
            });
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostSubmitAsync(int proposalId)
        {
            await TryUpdateModelAsync(SubmitSection, nameof(SubmitSection));

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            var sectionHandler = _sectionHandlers.Single(h => h.Id == SubmitSection.Section);
            if (!await AuthorizeAsync(proposal, FormSectionOperation.Submit(sectionHandler.ModelType)))
            {
                return Forbid();
            }

            if (!await sectionHandler.ValidateSubmissionAsync(this, proposal))
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            foreach (var approval in sectionHandler.GetAssociatedApprovals(proposal))
            {
                if (sectionHandler.IsAuthorityApplicable(proposal, approval.AuthorityRole))
                {
                    var authorities = await _authorityProvider.GetAuthoritiesByRoleAsync(proposal, approval.AuthorityRole);

                    // Is self approved (no approval required)?
                    if (!authorities.Any() || authorities.Any(authority => authority.Id == _userManager.GetUserId(User)))
                    {
                        if (!await sectionHandler.ValidateApprovalAsync(this, proposal))
                        {
                            await LoadFormAsync(proposal);
                            return Page();
                        }

                        approval.Status = ApprovalStatus.Approved;
                        approval.ValidatedBy = _userManager.GetUserId(User);
                    }
                    else
                    {
                        var primary = authorities.First();

                        await _emailService.SendEmailAsync(User, new ApprovalRequest
                        {
                            ApplicantName = _userManager.GetUserName(User),
                            ProposalTitle = proposal.Title,
                            SectionName = sectionHandler.DisplayName,
                            PageLink = Url.Page(null, null, new { proposalId }, "https", null, sectionHandler.Id)
                        }, new MailAddress(primary.Email, primary.DisplayName), true);

                        approval.Status = ApprovalStatus.ApprovalPending;
                    }
                }
                else
                {
                    approval.Status = ApprovalStatus.NotApplicable;
                }
            }

            await UpdateProposalAsync(proposal);
            await SendMailIfProposalApprovedAsync(proposal);

            return RedirectToPage(null, null, new { proposalId }, SubmitSection.Section);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostRetractAsync(int proposalId)
        {
            await TryUpdateModelAsync(RetractSection, nameof(RetractSection));

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            var sectionHandler = _sectionHandlers.Single(h => h.Id == RetractSection.Section);
            if (!await AuthorizeAsync(proposal, FormSectionOperation.Retract(sectionHandler.ModelType)))
            {
                return Forbid();
            }

            foreach (var approval in sectionHandler.GetAssociatedApprovals(proposal))
            {
                approval.Status = ApprovalStatus.NotSubmitted;
                approval.ValidatedBy = null;
            }

            InvalidateDependentSections(proposal, sectionHandler);

            await UpdateProposalAsync(proposal);

            return RedirectToPage(null, null, new { proposalId }, RetractSection.Section);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostApproveAsync(int proposalId)
        {
            await TryUpdateModelAsync(ApproveSection, nameof(ApproveSection));

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await ApproveOrRejectAsync(proposalId, true);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostRejectAsync(int proposalId)
        {
            await TryUpdateModelAsync(RejectSection, nameof(RejectSection));

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await ApproveOrRejectAsync(proposalId, false);
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

            await UpdateProposalAsync(proposal);
            await LoadSectionAsync(proposal, _sectionHandlers.Single(h => h.ModelType == typeof(ExperimentSectionModel)));

            Timestamp = proposal.Timestamp;
            ViewData["LabId"] = lab.Id;
            return Partial("Shared/_Lab", this);
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
                return NotFound();
            }

            proposal.Labs.Remove(lab);

            await UpdateProposalAsync(proposal);

            return new JsonResult(new
            {
                proposal.Timestamp
            });
        }

        private async Task<IActionResult> ApproveOrRejectAsync(int proposalId, bool approve)
        {
            var role = (ApprovalAuthorityRole) (approve ? ApproveSection.Role : RejectSection.Role);

            var (proposal, error) = await LoadProposalAsync(proposalId);
            if (proposal == null)
            {
                return error;
            }

            var approval = proposal.Approvals.Single(a => a.AuthorityRole == role);
            var sectionHandler = _sectionHandlers.Single(h => h.HasApprovalAuthorityRole(role));

            var operation = approve ? ApprovalOperation.Approve : ApprovalOperation.Reject;
            if (!await AuthorizeAsync(approval, operation))
            {
                return Forbid();
            }

            if (approve && !await sectionHandler.ValidateApprovalAsync(this, proposal))
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            approval.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            approval.ValidatedBy = _userManager.GetUserId(User);

            var invalidatedSections = approve ? null : InvalidateDependentSections(proposal, sectionHandler);

            await UpdateProposalAsync(proposal);

            var owner = await _userManager.GetUserByIdAsync(proposal.OwnerId);
            var ownerEmail = new MailAddress(owner.Email, owner.DisplayName);

            if (approve)
            {
                await _emailService.SendEmailAsync(User, new SectionApproved
                {
                    ApproverName = _userManager.GetUserName(User),
                    Remarks = ApproveSection.Remarks,
                    ProposalTitle = proposal.Title,
                    SectionName = sectionHandler.DisplayName,
                    PageLink = Url.Page(null, null, new {proposalId}, "https", null, sectionHandler.Id)
                }, ownerEmail, true);

                await SendMailIfProposalApprovedAsync(proposal);
            }
            else
            {
                await _emailService.SendEmailAsync(User, new SectionRejected
                {
                    ApproverName = _userManager.GetUserName(User),
                    Reason = RejectSection.Reason,
                    ProposalTitle = proposal.Title,
                    SectionName = sectionHandler.DisplayName,
                    InvalidatedSections = string.Join(", ", invalidatedSections?.Select(h => h.DisplayName)),
                    PageLink = Url.Page(null, null, new {proposalId}, "https", null, sectionHandler.Id)
                }, ownerEmail, true);
            }

            return RedirectToPage(null, null, new { proposalId }, sectionHandler.Id);
        }

        private async Task SendMailIfProposalApprovedAsync(Proposal proposal)
        {
            if (proposal.Approvals.All(a => a.Status == ApprovalStatus.Approved || a.Status == ApprovalStatus.NotApplicable))
            {
                var administration = await _authorityProvider.GetAdministrationAsync();
                var primary = administration.First();

                await _emailService.SendEmailAsync(User, new ProposalApproved
                {
                    ApplicantName = (await _userManager.GetUserByIdAsync(proposal.OwnerId)).DisplayName,
                    ProposalTitle = proposal.Title,
                    PageLink = Url.Page(null, null, new { ProposalId = proposal.Id }, "https")
                }, new MailAddress(primary.Email, primary.DisplayName));
            }
        }

        private ICollection<IFormSectionHandler> InvalidateDependentSections(Proposal proposal, IFormSectionHandler sectionHandler)
        {
            IEnumerable<IFormSectionHandler> FindDependentSections(IFormSectionHandler section)
            {
                var dependentSections = _sectionHandlers
                    .Where(h => h.NeedsApprovalBy(proposal).Any(section.HasApprovalAuthorityRole))
                    .ToList();

                return dependentSections.Concat(dependentSections.SelectMany(FindDependentSections));
            }

            var invalidatedSections = FindDependentSections(sectionHandler)
                .Distinct()
                .Where(h => h.GetAssociatedApprovals(proposal).Any(a => a.Status != ApprovalStatus.NotSubmitted))
                .ToList();

            foreach (var approval in invalidatedSections.SelectMany(h => h.GetAssociatedApprovals(proposal)))
            {
                approval.Status = ApprovalStatus.NotSubmitted;
                approval.ValidatedBy = null;
            }

            return invalidatedSections;
        }

        private async Task<(Proposal Proposal, IActionResult Error)> LoadProposalAsync(int proposalId)
        {
            var proposal = await _proposalDbContext.Proposals
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
                _logger.LogWarning("Received stale timestamp.");
            }

            ProposalId = proposal.Id;
            Timestamp = proposal.Timestamp;

            return (proposal, null);
        }

        private async Task LoadFormAsync(Proposal proposal)
        {
            Sections = _sectionHandlers.Select(h => h.GetModel(this)).ToList();

            foreach (var sectionHandler in _sectionHandlers)
            {
                await LoadSectionAsync(proposal, sectionHandler);
            }
        }

        private async Task LoadSectionAsync(Proposal proposal, IFormSectionHandler sectionHandler)
        {
            var userId = _userManager.GetUserId(User);

            await sectionHandler.LoadAsync(this, proposal);

            var model = sectionHandler.GetModel(this);
            model.CanEdit = await AuthorizeAsync(proposal, FormSectionOperation.Edit(sectionHandler.ModelType));
            model.CanSubmit = await AuthorizeAsync(proposal, FormSectionOperation.Submit(sectionHandler.ModelType));
            model.CanRetract = await AuthorizeAsync(proposal, FormSectionOperation.Retract(sectionHandler.ModelType));
            model.Approvals = await proposal.Approvals
                .Where(a => sectionHandler.HasApprovalAuthorityRole(a.AuthorityRole))
                .Select(async approval =>
                {
                    var authorities = await _authorityProvider.GetAuthoritiesByRoleAsync(proposal, approval.AuthorityRole);

                    ProjectDbUser authority;
                    if (approval.Status == ApprovalStatus.Approved || approval.Status == ApprovalStatus.Rejected)
                    {
                        // Show authority who approved the section at the time as opposed to current approval role
                        authority = await _userManager.GetUserByIdAsync(approval.ValidatedBy);
                    }
                    else
                    {
                        // Pick first from the list (if any)
                        authority = authorities.FirstOrDefault();
                    }

                    return new SectionApprovalModel
                    {
                        AuthorityRole = (ApprovalAuthorityRoleModel) approval.AuthorityRole,
                        AuthorityName = authority?.DisplayName,
                        AuthorityEmail = authority?.Email,
                        Status = (ApprovalStatusModel) approval.Status,

                        CanApprove = await AuthorizeAsync(approval, ApprovalOperation.Approve),
                        CanReject = await AuthorizeAsync(approval, ApprovalOperation.Reject),
                        IsAutoApproved = !authorities.Any(),
                        // TODO: supervisor?
                        IsSelfApproved = authorities.Any(a => a.Id == userId) && userId == proposal.OwnerId
                    };
                })
                .ToListAsync();
        }

        private async Task UpdateProposalAsync(Proposal proposal)
        {
            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);
            _proposalDbContext.Entry(proposal).State = EntityState.Modified;

            await _proposalDbContext.SaveChangesAsync();
        }

        private async Task<bool> AuthorizeAsync(Proposal proposal, IAuthorizationRequirement requirement)
        {
            return (await _authorizationService.AuthorizeAsync(User, proposal, requirement)).Succeeded;
        }

        private async Task<bool> AuthorizeAsync(Approval approval, IAuthorizationRequirement requirement)
        {
            return (await _authorizationService.AuthorizeAsync(User, approval, requirement)).Succeeded;
        }
    }
}