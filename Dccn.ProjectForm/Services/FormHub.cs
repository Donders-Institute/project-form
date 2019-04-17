using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Services
{
    [Authorize]
    public class FormHub : Hub<IFormHubClient>
    {
        private readonly ProposalDbContext _proposalDbContext;
        private readonly IAuthorizationService _authorizationService;

        public FormHub(ProposalDbContext proposalDbContext, IAuthorizationService authorizationService)
        {
            _proposalDbContext = proposalDbContext;
            _authorizationService = authorizationService;
        }

        public override async Task OnConnectedAsync()
        {
            if (!Context.GetHttpContext().Request.Query.TryGetValue("proposalId", out var proposalIdQuery))
            {
                Context.Abort();
                return;
            }

            var proposalIdString = proposalIdQuery.FirstOrDefault();
            if (proposalIdString == null || !int.TryParse(proposalIdString, out var proposalId))
            {
                Context.Abort();
                return;
            }

            var proposal = await _proposalDbContext.Proposals
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (!(await _authorizationService.AuthorizeAsync(Context.User, proposal, FormOperation.View)).Succeeded)
            {
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, proposalId.ToString());
            await base.OnConnectedAsync();
        }
    }

    public interface IFormHubClient
    {
        Task UpdateForm(int id, byte[] timestamp, string lastEditedBy, DateTime lastEditedOn, ICollection<string> changedSections);
    }
}