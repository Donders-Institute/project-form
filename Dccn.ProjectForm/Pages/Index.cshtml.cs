using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using Dccn.ProjectForm.Utils;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ProjectsDbContext _projectsDbContext;
        private readonly ProposalsDbContext _proposalsDbContext;
        private readonly IUserManager _userManager;

        public IndexModel(IAuthorizationService authorizationService, IAuthorityProvider authorityProvider, IEnumerable<IFormSectionHandler> sectionHandlers, ProjectsDbContext projectsDbContext, ProposalsDbContext proposalsDbContext, IUserManager userManager)
        {
            _authorizationService = authorizationService;
            _authorityProvider = authorityProvider;
            _sectionHandlers = sectionHandlers;
            _projectsDbContext = projectsDbContext;
            _proposalsDbContext = proposalsDbContext;
            _userManager = userManager;
        }

        public NewProposalModel NewProposal { get; set; }

        public bool IsSupervisor { get; private set; }
        public bool IsApprovalAuthority { get; private set; }
        public ICollection<ProposalModel> MyProposals { get; private set; }
        public ICollection<ProposalModel> SupervisedProposals { get; private set; }
        public ICollection<PendingApprovalModel> PendingApprovals { get; private set; }

        [UsedImplicitly]
        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var proposal = await _proposalsDbContext.Proposals.FindAsync(id);
            if (proposal == null)
            {
                return NotFound();
            }

            if (!(await _authorizationService.AuthorizeAsync(User, proposal, FormOperation.Delete)).Succeeded)
            {
                return Forbid();
            }

            _proposalsDbContext.Remove(proposal);
            await _proposalsDbContext.SaveChangesAsync();

            return RedirectToPage();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostCreateAsync([FromForm(Name = nameof(NewProposal))] NewProposalModel model)
        {
            var ownerId = _userManager.GetUserId(User);
            if (await _proposalsDbContext.Proposals.Where(p => p.OwnerId == ownerId).AnyAsync(p => p.Title == model.Title))
            {
                ModelState.AddModelError(string.Empty, "A proposal with the same name already exists.");
            }

            if (!await _projectsDbContext.Groups.AnyAsync(g => g.HeadId == model.SupervisorId))
            {
                ModelState.AddModelError(string.Empty, "The supervisor is not valid.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageAsync();
                return Page();
            }

            var proposal = new Proposal
            {
                LastEditedBy = ownerId,
                Title = model.Title,
                OwnerId = ownerId,
                SupervisorId = model.SupervisorId,
                Experimenters = new List<Experimenter>
                {
                    new Experimenter
                    {
                        UserId = ownerId
                    }
                },
                DataAccessRules = new List<StorageAccessRule>
                {
                    new StorageAccessRule
                    {
                        UserId = ownerId,
                        Role = StorageAccessRole.Manager
                    }
                },
                Approvals = EnumUtils.GetValues<ApprovalAuthorityRole>()
                    .Select(role => new Approval()
                    {
                        AuthorityRole = role
                    })
                    .ToList()
            };

            // Don't create duplicate entry if supervisor == owner
            if (model.SupervisorId != ownerId)
            {
                proposal.DataAccessRules.Add(new StorageAccessRule
                {
                    UserId = model.SupervisorId,
                    Role = StorageAccessRole.Viewer
                });
            }

            _proposalsDbContext.Add(proposal);
            await _proposalsDbContext.SaveChangesAsync();

            return RedirectToPage("Form", new {proposalId = proposal.Id});
        }

        private async Task LoadPageAsync()
        {
            var user = await _userManager.GetUserAsync(User, true);
            var userId = _userManager.GetUserId(User);

            MyProposals = await LoadProposalsAsync(_proposalsDbContext.Proposals.Where(p => p.OwnerId == userId));

            IsSupervisor = user.IsHead;
            if (IsSupervisor)
            {
                SupervisedProposals = await LoadProposalsAsync(_proposalsDbContext.Proposals.Where(p => p.SupervisorId == userId));
            }

            var supervisors = await _projectsDbContext.Groups
                .Where(g => !string.IsNullOrEmpty(g.HeadId)) // Workaround: stored in db as empty strings
                .Select(g => new SelectListItem($"{g.Head.DisplayName} ({g.Description})", g.Head.Id))
                .ToListAsync();

            var authorityRoles = _authorityProvider.GetAuthorityRoles(userId).ToList();
            if (IsSupervisor)
            {
                authorityRoles.Add(ApprovalAuthorityRole.Supervisor);
            }

            IsApprovalAuthority = authorityRoles.Any();
            if (IsApprovalAuthority)
            {
                var approvalsQueryResult = await _proposalsDbContext.Approvals
                    .Where(a => authorityRoles.Contains(a.AuthorityRole))
                    .Where(a => a.Status == ApprovalStatus.ApprovalPending)
                    .Select(a => new
                    {
                        a.ProposalId,
                        ProposalOwnerId = a.Proposal.OwnerId,
                        ProposalTitle = a.Proposal.Title,
                        a.AuthorityRole
                    })
                    .ToListAsync();

                PendingApprovals = await approvalsQueryResult
                    .Select(async a => new PendingApprovalModel
                    {
                        ProposalId = a.ProposalId,
                        SectionId = _sectionHandlers.FirstOrDefault(s => s.HasApprovalAuthorityRole(a.AuthorityRole))?.Id,
                        ProposalOwnerName = (await _projectsDbContext.Users.FindAsync(a.ProposalOwnerId)).DisplayName,
                        ProposalTitle = a.ProposalTitle,
                        Role = (ApprovalAuthorityRoleModel) a.AuthorityRole
                    })
                    .ToListAsync();
            }

            NewProposal = new NewProposalModel
            {
                SupervisorId = user.Group.HeadId,
                Supervisors = supervisors
            };
        }

        private async Task<ICollection<ProposalModel>> LoadProposalsAsync(IQueryable<Proposal> query)
        {
            var queryResult = await query
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.ProjectId,
                    p.OwnerId,
                    p.SupervisorId,
                    p.CreatedOn,
                    p.LastEditedOn,
                    p.LastEditedBy,
                    NotSubmitted = p.Approvals.Count(a => a.Status == ApprovalStatus.NotSubmitted),
                    Pending = p.Approvals.Count(a => a.Status == ApprovalStatus.ApprovalPending),
                    Approved = p.Approvals.Count(a => a.Status == ApprovalStatus.Approved),
                    Rejected = p.Approvals.Count(a => a.Status == ApprovalStatus.Rejected)
                })
                .OrderByDescending(p => p.LastEditedOn)
                .ToListAsync();

            return await queryResult
                .Select(async p => new ProposalModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ProjectId = p.ProjectId,
                    OwnerName = (await _projectsDbContext.Users.FindAsync(p.OwnerId)).DisplayName,
                    SupervisorName = (await _projectsDbContext.Users.FindAsync(p.SupervisorId)).DisplayName,
                    CreatedOn = p.CreatedOn,
                    LastEditedOn = p.LastEditedOn,
                    LastEditedBy = (await _projectsDbContext.Users.FindAsync(p.LastEditedBy)).DisplayName,
                    NotSubmitted = p.NotSubmitted,
                    Pending = p.Pending,
                    Approved = p.Approved,
                    Rejected = p.Rejected
                })
                .ToListAsync();
        }
    }
}