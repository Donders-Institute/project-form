﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
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
        private readonly ProposalDbContext _proposalDbContext;
        private readonly IUserManager _userManager;

        public IndexModel(IAuthorizationService authorizationService, IAuthorityProvider authorityProvider, IEnumerable<IFormSectionHandler> sectionHandlers, ProposalDbContext proposalDbContext, IUserManager userManager)
        {
            _authorizationService = authorizationService;
            _authorityProvider = authorityProvider;
            _sectionHandlers = sectionHandlers;
            _proposalDbContext = proposalDbContext;
            _userManager = userManager;
        }

        public NewProposalModel NewProposal { get; set; }

        public bool IsSupervisor { get; private set; }
        public bool IsAdministration { get; private set; }
        public bool IsApprovalAuthority { get; private set; }
        public ICollection<ProposalModel> MyProposals { get; private set; }
        public ICollection<ProposalModel> SupervisedProposals { get; private set; }
        public ICollection<ProposalModel> FinishedProposals { get; private set; }
        public ICollection<ApprovalModel> Approvals { get; private set; }

        [UsedImplicitly]
        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var proposal = await _proposalDbContext.Proposals.FindAsync(id);
            if (proposal == null)
            {
                return NotFound();
            }

            if (!(await _authorizationService.AuthorizeAsync(User, proposal, FormOperation.Delete)).Succeeded)
            {
                return Forbid();
            }

            _proposalDbContext.Remove(proposal);
            await _proposalDbContext.SaveChangesAsync();

            return RedirectToPage();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostCreateAsync([FromForm(Name = nameof(NewProposal))] NewProposalModel model)
        {
            var ownerId = _userManager.GetUserId(User);
            if (await _proposalDbContext.Proposals.Where(p => p.OwnerId == ownerId).AnyAsync(p => p.Title == model.Title))
            {
                ModelState.AddModelError(string.Empty, "A proposal with the same name already exists.");
            }

            if (!await _userManager.QueryGroups().Where(g => !g.Hidden).AnyAsync(g => g.HeadId == model.SupervisorId))
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
            if (model.SupervisorId != ownerId)
            {
                proposal.StorageAccessRules.Add(new StorageAccessRule
                {
                    UserId = model.SupervisorId,
                    Role = StorageAccessRole.Viewer
                });
            }

            _proposalDbContext.Proposals.Add(proposal);
            await _proposalDbContext.SaveChangesAsync();

            return RedirectToPage("Form", new {proposalId = proposal.Id});
        }

        private async Task LoadPageAsync()
        {
            var user = await _userManager.GetUserAsync(User, true);
            var userId = _userManager.GetUserId(User);

            MyProposals = await LoadProposalsAsync(_proposalDbContext.Proposals.Where(p => p.OwnerId == userId));

            IsAdministration = _userManager.IsInRole(User, Role.Administration);
            if (IsAdministration)
            {
                FinishedProposals = await LoadProposalsAsync(_proposalDbContext.Proposals.Where(p =>
                    p.ProjectId == null && p.Approvals.All(a =>
                        a.Status == ApprovalStatus.Approved || a.Status == ApprovalStatus.NotApplicable)));
            }

            IsSupervisor = _userManager.IsInRole(User, Role.Supervisor);
            if (IsSupervisor)
            {
                SupervisedProposals = await LoadProposalsAsync(_proposalDbContext.Proposals.Where(p => p.SupervisorId == userId));
            }

            var supervisors = await _userManager.QueryGroups()
                .Where(g => !g.Hidden)
                .Where(g => !string.IsNullOrEmpty(g.HeadId)) // Workaround: stored in db as empty strings
                .Select(g => new SelectListItem($"{g.Head.DisplayName} ({g.Description})", g.Head.Id))
                .ToListAsync();

            var authorityRoles = _authorityProvider.GetAuthorityRoles(userId).ToList();
            IsApprovalAuthority = authorityRoles.Any() || IsSupervisor;
            if (IsApprovalAuthority)
            {
                var approvalsQueryResult = await _proposalDbContext.Approvals
                    .Where(a => authorityRoles.Contains(a.AuthorityRole) || a.AuthorityRole == ApprovalAuthorityRole.Supervisor && a.Proposal.SupervisorId == userId)
                    .Where(a => a.Status == ApprovalStatus.ApprovalPending || a.Status == ApprovalStatus.Rejected || a.Status == ApprovalStatus.Approved)
                    .Select(a => new
                    {
                        a.ProposalId,
                        ProposalOwnerId = a.Proposal.OwnerId,
                        ProposalTitle = a.Proposal.Title,
                        a.AuthorityRole,
                        a.Status
                    })
                    .ToListAsync();

                var unorderedApprovals = await approvalsQueryResult
                    .Select(async a => new ApprovalModel
                    {
                        ProposalId = a.ProposalId,
                        SectionId = _sectionHandlers.FirstOrDefault(s => s.HasApprovalAuthorityRole(a.AuthorityRole))?.Id,
                        ProposalOwnerName = (await _userManager.GetUserByIdAsync(a.ProposalOwnerId)).DisplayName,
                        ProposalTitle = a.ProposalTitle,
                        Role = (ApprovalAuthorityRoleModel) a.AuthorityRole,
                        Status = (ApprovalStatusModel) a.Status
                    })
                    .ToListAsync();

                Approvals = unorderedApprovals
                    .Where(a => a.Status == ApprovalStatusModel.ApprovalPending)
                    .Concat(unorderedApprovals.Where(a => a.Status != ApprovalStatusModel.ApprovalPending))
                    .ToList();
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
                    p.LastEditedBy
                })
                .OrderByDescending(p => p.LastEditedOn)
                .ToListAsync();

            return await queryResult
                .Select(async p => new ProposalModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ProjectId = p.ProjectId,
                    OwnerName = (await _userManager.GetUserByIdAsync(p.OwnerId)).DisplayName,
                    SupervisorName = (await _userManager.GetUserByIdAsync(p.SupervisorId)).DisplayName,
                    CreatedOn = p.CreatedOn,
                    LastEditedOn = p.LastEditedOn,
                    LastEditedBy = (await _userManager.GetUserByIdAsync(p.LastEditedBy)).DisplayName
                })
                .ToListAsync();
        }
    }
}