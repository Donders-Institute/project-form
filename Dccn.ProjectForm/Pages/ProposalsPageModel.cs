using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Pages
{
    public class ProposalsPageModel : PageModel
    {
        protected ProposalsPageModel(ProposalDbContext proposalDbContext, IUserManager userManager)
        {
            ProposalDbContext = proposalDbContext;
            UserManager = userManager;
        }

        public ICollection<ProposalModel> Proposals { get; private set; }

        protected ProposalDbContext ProposalDbContext { get; }
        protected IUserManager UserManager { get; }

        protected async Task LoadProposalsAsync(Func<IQueryable<Proposal>, IQueryable<Proposal>> query)
        {
            var queryResult = await query(ProposalDbContext.Proposals)
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

            Proposals = await queryResult
                .Select(async p => new ProposalModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ProjectId = p.ProjectId,
                    OwnerName = (await UserManager.GetUserByIdAsync(p.OwnerId)).DisplayName,
                    SupervisorName = (await UserManager.GetUserByIdAsync(p.SupervisorId)).DisplayName,
                    CreatedOn = p.CreatedOn,
                    LastEditedOn = p.LastEditedOn,
                    LastEditedBy = (await UserManager.GetUserByIdAsync(p.LastEditedBy)).DisplayName
                })
                .ToListAsync();
        }
    }
}