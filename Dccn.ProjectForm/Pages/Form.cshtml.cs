using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Email.Models;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Pages
{
    public class FormModel : PageModel
    {
        private const string ProjectsDatabaseProjectBaseUrl = "https://intranet.donders.ru.nl/apps/projects/projects/view/";

        private readonly ProposalDbContext _proposalDbContext;
        private readonly IUserManager _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEmailService _emailService;
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;
        private readonly IAuthorityProvider _authorityProvider;
        private readonly IProjectDbExporter _exporter;

        public FormModel(ProposalDbContext proposalDbContext, IUserManager userManager, IAuthorizationService authorizationService, IEmailService emailService, IEnumerable<IFormSectionHandler> sectionHandlers, IAuthorityProvider authorityProvider, IProjectDbExporter exporter)
        {
            _proposalDbContext = proposalDbContext;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _emailService = emailService;
            _sectionHandlers = sectionHandlers;
            _authorityProvider = authorityProvider;
            _exporter = exporter;
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
        public ExportModel Export { get; private set; }

        [BindProperty]
        [BindRequired]
        [Required]
        public byte[] Timestamp { get; set; }

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
                Success = ModelState.IsValid,
                Errors = new SerializableError(ModelState)
            });
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostSubmitAsync(int proposalId, string sectionId)
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

            if (!await sectionHandler.ValidateSubmissionAsync(this, proposal))
            {
                await LoadFormAsync(proposal);
                return Page();
            }

            foreach (var approval in sectionHandler.GetAssociatedApprovals(proposal))
            {
                if (sectionHandler.IsAuthorityApplicable(proposal, approval.AuthorityRole))
                {
                    var authorities = await _authorityProvider.GetAuthoritiesAsync(proposal, approval.AuthorityRole);
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
                        var recipients = authorities
                            .Select(authority => new MailAddress(authority.Email, authority.DisplayName))
                            .ToArray();

                        await _emailService.SendEmailAsync(User, new ApprovalRequest
                        {
                            ApplicantName = _userManager.GetUserName(User),
                            ProposalTitle = proposal.Title,
                            SectionName = sectionHandler.DisplayName,
                            PageLink = Url.Page(null, null, new { proposalId }, "https", null, sectionHandler.Id)
                        }, recipients);

                        approval.Status = ApprovalStatus.ApprovalPending;
                    }
                }
                else
                {
                    approval.Status = ApprovalStatus.NotApplicable;
                }
            }

            error = await TrySaveChangesAsync(proposal);
            if (error != null)
            {
                return error;
            }

            await SendMailIfProposalApprovedAsync(proposal);

//            var writer = new StringWriter();
//            var builder = new HtmlContentBuilder()
//                .Append("An e-mail with a request for approval has been sent ")
//                .AppendHtml(link)
//                .Append(".")
//                .WriteTo(writer, HtmlEncoder.Default);
//
//            TempData["Message"] = writer.ToString();
//            TempData["MessageType"] = "success";

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

            InvalidateDependentSections(proposal, sectionHandler);

            error = await TrySaveChangesAsync(proposal);
            if (error != null)
            {
                return error;
            }

            return RedirectToPage(null, null, new { proposalId }, sectionId);
        }

        [UsedImplicitly]
        public Task<IActionResult> OnPostApproveAsync(int proposalId, [Required] ApprovalAuthorityRole role)
        {
            return ApproveOrRejectAsync(proposalId, role, true);
        }

        [UsedImplicitly]
        public Task<IActionResult> OnPostRejectAsync(int proposalId, [Required] ApprovalAuthorityRole role)
        {
            return ApproveOrRejectAsync(proposalId, role, false);
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
//            proposal.LastEditedOn = DateTime.Now;
//            proposal.LastEditedBy = _userManager.GetUserId(User);

            error = await TrySaveChangesAsync(proposal);
            if (error != null)
            {
                return error;
            }

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
//            proposal.LastEditedOn = DateTime.Now;
//            proposal.LastEditedBy = _userManager.GetUserId(User);

            error = await TrySaveChangesAsync(proposal);
            if (error != null)
            {
                return error;
            }

            return new JsonResult(new
            {
                proposal.Timestamp
            });
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostExportAsync(int proposalId, [FromForm(Name = nameof(Export))] ExportModel exportInfo)
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

            if (!await AuthorizeAsync(proposal, FormOperation.Export))
            {
                return Forbid();
            }

            await _exporter.Export(proposal, exportInfo.SourceId);

            error = await TrySaveChangesAsync(proposal);
            if (error != null)
            {
                return error;
            }

            var link = new TagBuilder("a");
            link.Attributes.Add("target", "_blank");
            var baseUri = new Uri(ProjectsDatabaseProjectBaseUrl);
            link.Attributes.Add("href", new Uri(baseUri,proposal.ProjectId).ToString());
            link.InnerHtml.Append(proposal.ProjectId);

            var writer = new StringWriter();
            new HtmlContentBuilder()
                .Append("The project proposal was succesfully exported to the Project Database as ")
                .AppendHtml(link)
                .Append(".")
                .WriteTo(writer, HtmlEncoder.Default);

            TempData["Message"] = writer.ToString();
            TempData["MessageType"] = "success";

            return RedirectToPage("Index");
        }

        private async Task<IActionResult> ApproveOrRejectAsync(int proposalId, ApprovalAuthorityRole role, bool approve)
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

            error = await TrySaveChangesAsync(proposal);
            if (error != null)
            {
                return error;
            }

            var owner = await _userManager.GetUserByIdAsync(proposal.OwnerId);
            var ownerEmail = new MailAddress(owner.Email, owner.DisplayName);

            if (approve)
            {
                await _emailService.SendEmailAsync(User, new SectionApproved
                {
                    ApproverName = _userManager.GetUserName(User),
                    ProposalTitle = proposal.Title,
                    SectionName = sectionHandler.DisplayName,
                    PageLink = Url.Page(null, null, new {proposalId}, "https", null, sectionHandler.Id)
                }, ownerEmail);

                await SendMailIfProposalApprovedAsync(proposal);
            }
            else
            {
                await _emailService.SendEmailAsync(User, new SectionRejected
                {
                    ApproverName = _userManager.GetUserName(User),
                    ProposalTitle = proposal.Title,
                    SectionName = sectionHandler.DisplayName,
                    InvalidatedSections = string.Join(", ", invalidatedSections?.Select(h => h.DisplayName)),
                    PageLink = Url.Page(null, null, new {proposalId}, "https", null, sectionHandler.Id)
                }, ownerEmail);
            }

            return RedirectToPage(null, null, new { proposalId }, sectionHandler.Id);
        }

        private async Task SendMailIfProposalApprovedAsync(Proposal proposal)
        {
            if (proposal.Approvals.All(a => a.Status == ApprovalStatus.Approved || a.Status == ApprovalStatus.NotApplicable))
            {
                var administration = await _authorityProvider.GetAdministrationAsync(proposal);

                await _emailService.SendEmailAsync(User, new ProposalApproved
                {
                    ApplicantName = _userManager.GetUserName(User),
                    ProposalTitle = proposal.Title,
                    PageLink = Url.Page(null, null, new {proposal.Id}, "https")
                }, administration.Select(a => new MailAddress(a.Email, a.DisplayName)).ToArray());
            }
        }

        private ICollection<IFormSectionHandler> InvalidateDependentSections(Proposal proposal, IFormSectionHandler sectionHandler)
        {
            IEnumerable<IFormSectionHandler> FindDependentSections(IFormSectionHandler section)
            {
                var dependantSections = _sectionHandlers
                    .Where(h => h.NeedsApprovalBy(proposal).Any(section.HasApprovalAuthorityRole))
                    .ToList();

                return dependantSections
                    .Concat(dependantSections.SelectMany(FindDependentSections))
                    .Distinct();
            }

            ICollection<IFormSectionHandler> invalidatedSections = FindDependentSections(sectionHandler).ToList();
            var invalidatedApprovals = invalidatedSections
                .SelectMany(h => h.GetAssociatedApprovals(proposal))
                .Where(a => a.Status != ApprovalStatus.NotSubmitted)
                .Distinct();

            foreach (var invalidatedApproval in invalidatedApprovals)
            {
                invalidatedApproval.Status = ApprovalStatus.NotSubmitted;
                invalidatedApproval.ValidatedBy = null;
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
                return (null, new ConflictObjectResult(new
                {
                    LastEditedBy = (await _userManager.GetUserByIdAsync(proposal.LastEditedBy)).DisplayName,
                    LastEditedOn = proposal.LastEditedOn.ToString("g")
                }));
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
                foreach (var approvalInfo in model.Approvals)
                {
                    var approval = approvalInfo.RawApproval;
                    approvalInfo.CanApprove = await AuthorizeAsync(approval, ApprovalOperation.Approve);
                    approvalInfo.CanReject = await AuthorizeAsync(approval, ApprovalOperation.Reject);

                    var authorities = await _authorityProvider.GetAuthoritiesAsync(proposal, approval.AuthorityRole);
                    var userId = _userManager.GetUserId(User);
                    approvalInfo.IsAutoApproved = !authorities.Any();
                    approvalInfo.IsSelfApproved = authorities.Any(authority => authority.Id == userId) && userId == proposal.OwnerId;
                }
            }

            if (await AuthorizeAsync(proposal, FormOperation.Export))
            {
                Export = new ExportModel
                {
                    SourceId = Funding.FinancialCode
                };
            }
        }

        private async Task<IActionResult> StoreFormAsync(Proposal proposal, string sectionId = null)
        {
            var updated = false;
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
                updated = true;
            }

            if (updated)
            {
//                proposal.LastEditedOn = DateTime.Now;
//                proposal.LastEditedBy = _userManager.GetUserId(User);

                return await TrySaveChangesAsync(proposal);
            }

            return null;
        }

        private async Task<IActionResult> TrySaveChangesAsync(Proposal proposal)
        {
            proposal.LastEditedOn = DateTime.Now;
            proposal.LastEditedBy = _userManager.GetUserId(User);

            try
            {
                await _proposalDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException exception)
            {
                var proposalEntity = exception.Entries.Single(entity => entity.Entity is Proposal);
                if (proposalEntity == null)
                {
                    return new ConflictResult();
                }

                var dbProposal = (Proposal) (await proposalEntity.GetDatabaseValuesAsync()).ToObject();
                return new ConflictObjectResult(new
                {
                    LastEditedBy = (await _userManager.GetUserByIdAsync(dbProposal.LastEditedBy)).DisplayName,
                    LastEditedOn = dbProposal.LastEditedOn.ToString("g")
                });
            }

            return null;
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