using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Utils;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Pages
{
    public class MyProposalsModel : ProposalsPageModel
    {
        private readonly IAuthorizationService _authorizationService;

        public MyProposalsModel(IAuthorizationService authorizationService, ProposalsDbContext proposalsDbContext, IUserManager userManager) : base(proposalsDbContext, userManager)
        {
            _authorizationService = authorizationService;
        }

        public NewProposalModel NewProposal { get; set; }

        [UsedImplicitly]
        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var proposal = await ProposalsDbContext.Proposals.FindAsync(id);
            if (proposal == null)
            {
                return NotFound();
            }

            if (!(await _authorizationService.AuthorizeAsync(User, proposal, FormOperation.Delete)).Succeeded)
            {
                return Forbid();
            }

            ProposalsDbContext.Remove(proposal);
            await ProposalsDbContext.SaveChangesAsync();

            return RedirectToPage();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostCreateAsync([FromForm(Name = nameof(NewProposal))] NewProposalModel model)
        {
            var ownerId = UserManager.GetUserId(User);
            if (await ProposalsDbContext.Proposals.Where(p => p.OwnerId == ownerId).AnyAsync(p => p.Title == model.Title))
            {
                ModelState.AddModelError(string.Empty, "A proposal with the same name already exists.");
            }

            if (!await UserManager.QueryGroups().AnyAsync(g => g.HeadId == model.SupervisorId))
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

            ProposalsDbContext.Proposals.Add(proposal);
            await ProposalsDbContext.SaveChangesAsync();

            return RedirectToPage("Form", new {proposalId = proposal.Id});
        }

        private async Task LoadPageAsync()
        {
            var user = await UserManager.GetUserAsync(User, true);
            var userId = UserManager.GetUserId(User);

            await LoadProposalsAsync(query => query.Where(p => p.OwnerId == userId));

            var queryResult = await UserManager.QueryGroups()
                .Where(g => !string.IsNullOrEmpty(g.HeadId)) // Workaround: stored in db as empty strings
                .Select(g => new {g.Head, g.Description})
                .ToListAsync();

            var supervisors = queryResult
                .Select(g => new SelectListItem($"{g.Head.DisplayName} ({g.Description})", g.Head.Id))
                .ToList();

            NewProposal = new NewProposalModel
            {
                SupervisorId = user.Group.HeadId,
                Supervisors = supervisors
            };
        }
    }
}