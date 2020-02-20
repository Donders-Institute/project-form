using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using Dccn.ProjectForm.Utils;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IAuthorityProvider _authorityProvider;
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;
        private readonly ProposalDbContext _proposalDbContext;
        private readonly IUserManager _userManager;
        private readonly IProjectDbExporter _exporter;

        public IndexModel(IAuthorizationService authorizationService, IAuthorityProvider authorityProvider, IEnumerable<IFormSectionHandler> sectionHandlers, ProposalDbContext proposalDbContext, IUserManager userManager, IProjectDbExporter exporter)
        {
            _authorizationService = authorizationService;
            _authorityProvider = authorityProvider;
            _sectionHandlers = sectionHandlers;
            _proposalDbContext = proposalDbContext;
            _userManager = userManager;
            _exporter = exporter;
        }

        public NewProposalModel NewProposal { get; } = new NewProposalModel();
        public ExportProposalModel ExportProposal { get; } = new ExportProposalModel();
        public DeleteProposalModel DeleteProposal { get; } = new DeleteProposalModel();

        public bool IsSupervisor { get; private set; }
        public bool IsAdministration { get; private set; }
        public bool IsApprovalAuthority { get; private set; }
        public int ApprovalAuthorityRoleCount { get; private set; }
        public ICollection<ProposalModel> MyProposals { get; } = ImmutableList<ProposalModel>.Empty;
        public ICollection<ProposalModel> SupervisedProposals { get; } = ImmutableList<ProposalModel>.Empty;
        public ICollection<ProposalModel> AllProposals { get; } = ImmutableList<ProposalModel>.Empty;
        public ICollection<ApprovalModel> Approvals { get; } = ImmutableList<ApprovalModel>.Empty;
        public Uri ProjectDbBaseUrl { get; } = new Uri("https://intranet.donders.ru.nl/apps/projects/projects/view/");

        [UsedImplicitly]
        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostCreateAsync()
        {
            NewProposal.Submitted = true;

            if (!await TryUpdateModelAsync(NewProposal, nameof(NewProposal)))
            {
                await LoadPageAsync();
                return Page();
            }

            var ownerId = _userManager.GetUserId(User);
            if (await _proposalDbContext.Proposals.AnyAsync(p => p.Title == NewProposal.Title))
            {
                ModelState.AddModelError(nameof(NewProposal) + "." + nameof(NewProposal.Title), "A project proposal with the same title already exists.");
            }

            if (!await _userManager.Groups.AnyAsync(g => g.HeadId == NewProposal.SupervisorId))
            {
                ModelState.AddModelError(nameof(NewProposal) + "." + nameof(NewProposal.SupervisorId), "The supervisor is not valid.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageAsync();
                return Page();
            }

            var proposal = new Proposal
            {
                LastEditedBy = ownerId,
                Title = NewProposal.Title,
                OwnerId = ownerId,
                SupervisorId = NewProposal.SupervisorId,
                Experimenters = new List<Experimenter>
                {
                    new Experimenter
                    {
                        UserId = ownerId
                    }
                },
                StorageAccessRules = new List<StorageAccessRule>
                {
                    new StorageAccessRule
                    {
                        UserId = ownerId,
                        Role = StorageAccessRole.Manager
                    }
                },
                Approvals = EnumUtils.GetValues<ApprovalAuthorityRole>()
                    .Select(role => new Approval
                    {
                        AuthorityRole = role
                    })
                    .ToList()
            };

            // Don't create duplicate entry if supervisor == owner
            if (NewProposal.SupervisorId != ownerId)
            {
                proposal.StorageAccessRules.Add(new StorageAccessRule
                {
                    UserId = NewProposal.SupervisorId,
                    Role = StorageAccessRole.Viewer
                });
            }

            _proposalDbContext.Proposals.Add(proposal);
            await _proposalDbContext.SaveChangesAsync();

//            TempData["Message"] = HtmlEncoder.Default.Encode("The project proposal was created succesfully.");
//            TempData["MessageType"] = "success";

            return RedirectToPage("Form", new { ProposalId = proposal.Id });
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            await TryUpdateModelAsync(DeleteProposal, nameof(DeleteProposal));

            if (!ModelState.IsValid)
            {
                await LoadPageAsync();
                return Page();
            }

            var proposal = await _proposalDbContext.Proposals.FindAsync(DeleteProposal.ProposalId);
            if (proposal == null)
            {
                ModelState.AddModelError(string.Empty, "The project proposal does not exist.");
                await LoadPageAsync();
                return Page();
            }

            if (!(await _authorizationService.AuthorizeAsync(User, proposal, FormOperation.Delete)).Succeeded)
            {
                return Forbid();
            }

            _proposalDbContext.Remove(proposal);
            await _proposalDbContext.SaveChangesAsync();

            TempData["Message"] = HtmlEncoder.Default.Encode("The project proposal was deleted.");
            TempData["MessageType"] = "success";

            return RedirectToPage();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostExportAsync()
        {
            await TryUpdateModelAsync(ExportProposal, nameof(ExportProposal));

            if (!ModelState.IsValid)
            {
                await LoadPageAsync();
                return Page();
            }

            var proposal = await _proposalDbContext.Proposals
                .Include(p => p.Approvals)
                .Include(p => p.Labs)
                .Include(p => p.Experimenters)
                .Include(p => p.StorageAccessRules)
                .FirstOrDefaultAsync(p => p.Id == ExportProposal.ProposalId);

            if (proposal == null)
            {
                ModelState.AddModelError(string.Empty, "The project proposal does not exist.");
                await LoadPageAsync();
                return Page();
            }

            if (!(await _authorizationService.AuthorizeAsync(User, proposal, FormOperation.Export)).Succeeded)
            {
                return Forbid();
            }

            if (!await _exporter.Export(proposal, ExportProposal.SourceId))
            {
                ModelState.AddModelError(string.Empty, "The funding source needs to be created first.");
                await LoadPageAsync();
                return Page();
            }

            proposal.LastEditedBy = _userManager.GetUserId(User);
            proposal.LastEditedOn = DateTime.Now;
            await _proposalDbContext.SaveChangesAsync();

            var link = new TagBuilder("a");
            link.Attributes.Add("target", "_blank");
            link.Attributes.Add("href", new Uri(ProjectDbBaseUrl, proposal.ProjectId).ToString());
            link.InnerHtml.Append(proposal.ProjectId);

            var writer = new StringWriter();
            new HtmlContentBuilder()
                .Append("The project proposal was successfully exported to the Project Database as ")
                .AppendHtml(link)
                .Append(".")
                .WriteTo(writer, HtmlEncoder.Default);

            TempData["Message"] = writer.ToString();
            TempData["MessageType"] = "success";

            return RedirectToPage();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnGetMyProposalsAsync()
        {
            var userId = _userManager.GetUserId(User);
            var proposals = await LoadProposalsAsync(_proposalDbContext.Proposals.Where(p => p.OwnerId == userId));
            return new JsonResult(proposals);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnGetSupervisedProposalsAsync()
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "RequireSupervisorRole")).Succeeded)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User);
            var proposals = await LoadProposalsAsync(_proposalDbContext.Proposals.Where(p => p.SupervisorId == userId));
            return new JsonResult(proposals);
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnGetAllProposalsAsync()
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "RequireAdministrationRole")).Succeeded)
            {
                return Forbid();
            }

            var proposals = await LoadProposalsAsync(_proposalDbContext.Proposals);
            return new JsonResult(proposals);
        }

        public async Task<IActionResult> OnGetApprovalsAsync()
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "RequireAuthorityRole")).Succeeded)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User);
            var authorityRoles = _authorityProvider.GetAuthorityRoles(userId).ToList();

            var approvalsQueryResult = await _proposalDbContext.Approvals
                .Where(a => authorityRoles.Contains(a.AuthorityRole) || a.AuthorityRole == ApprovalAuthorityRole.Supervisor && a.Proposal.SupervisorId == userId)
                .Where(a => a.Status == ApprovalStatus.ApprovalPending || a.Status == ApprovalStatus.Rejected || a.Status == ApprovalStatus.Approved)
                .OrderByDescending(a => a.Proposal.LastEditedOn)
                .Select(a => new
                {
                    a.ProposalId,
                    ProposalOwnerId = a.Proposal.OwnerId,
                    ProposalTitle = a.Proposal.Title,
                    a.AuthorityRole,
                    a.Status
                })
                .ToListAsync();

            var ownerNames = await _userManager.GetUserNamesForIdsAsync(approvalsQueryResult.Select(p => p.ProposalOwnerId));
            var approvals = approvalsQueryResult
                .Select(a => new ApprovalModel
                {
                    ProposalId = a.ProposalId,
                    SectionId = _sectionHandlers.First(s => s.HasApprovalAuthorityRole(a.AuthorityRole)).Id,
                    ProposalOwnerName = ownerNames[a.ProposalOwnerId],
                    ProposalTitle = a.ProposalTitle,
                    Role = (ApprovalAuthorityRoleModel) a.AuthorityRole,
                    RoleType = _authorityProvider.IsPrimaryAuthorityForRole(userId, a.AuthorityRole) ? ApprovalRoleTypeModel.Primary : ApprovalRoleTypeModel.Secondary,
                    Status = (ApprovalStatusModel) a.Status
                })
                .ToList();

            return new JsonResult(approvals);
        }

        private async Task LoadPageAsync()
        {
            var user = await _userManager.GetUserAsync(User, true);
            var userId = _userManager.GetUserId(User);

            IsAdministration = _userManager.IsInRole(User, Role.Administration);
            IsSupervisor = _userManager.IsInRole(User, Role.Supervisor);
            IsApprovalAuthority = _userManager.IsInRole(User, Role.Authority);
            ApprovalAuthorityRoleCount = _authorityProvider.GetAuthorityRoles(userId).Count();
            if (IsSupervisor)
            {
                ApprovalAuthorityRoleCount++;
            }

            if (NewProposal.SupervisorId == null)
            {
                NewProposal.SupervisorId = user.Group.HeadId;
            }

            NewProposal.Supervisors = await _userManager.Groups
                .Where(g => !g.Hidden)
                .Where(g => !string.IsNullOrEmpty(g.HeadId)) // Workaround: stored in db as empty strings
                .Select(g => new SelectListItem($"{g.Head.DisplayName} ({g.Description})", g.Head.Id))
                .ToListAsync();;
        }

        private async Task<ICollection<ProposalModel>> LoadProposalsAsync(IQueryable<Proposal> query)
        {
            var queryResult = await query
                .OrderByDescending(p => p.LastEditedOn)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.ProjectId,
                    p.FinancialCode,
                    p.OwnerId,
//                    p.SupervisorId,
//                    p.CreatedOn,
//                    p.LastEditedOn,
//                    p.LastEditedBy,
                    TotalApprovalCount = p.Approvals.Count(a => a.Status != ApprovalStatus.NotApplicable),
                    ApprovedCount = p.Approvals.Count(a => a.Status == ApprovalStatus.Approved)
                })
                .ToListAsync();

            var userIds = queryResult.Select(p => p.OwnerId).Distinct();
//                .Union(queryResult.Select(p => p.SupervisorId))
//                .Union(queryResult.Select(p => p.LastEditedBy));

            var userNames = await _userManager.GetUserNamesForIdsAsync(userIds);
//                .Where(user => userIds.Contains(user.Id))
//                .ToDictionaryAsync(user => user.Id, user => user.DisplayName);

            return queryResult
                .Select(p => new ProposalModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ProjectId = p.ProjectId,
                    SourceId = p.FinancialCode,
                    OwnerName = userNames[p.OwnerId],
//                    SupervisorName = users[p.SupervisorId].DisplayName,
//                    CreatedOn = p.CreatedOn,
//                    LastEditedOn = p.LastEditedOn,
//                    LastEditedBy = users[p.LastEditedBy].DisplayName,
                    Status = p.ProjectId == null
                        ? p.ApprovedCount == p.TotalApprovalCount
                            ? ProposalStatusModel.Approved
                            : ProposalStatusModel.InProgress
                        : ProposalStatusModel.Completed,
                    ApprovedCount = p.ApprovedCount,
                    TotalApprovalCount = p.TotalApprovalCount
                })
                .ToList();
        }
    }
}