using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class GeneralSectionHandler : FormSectionHandlerBase<GeneralSectionModel>
    {
        private readonly IUserManager _userManager;

        public GeneralSectionHandler(IServiceProvider serviceProvider, IUserManager userManager)
            : base(serviceProvider, m => m.General)
        {
            _userManager = userManager;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Supervisor};

        protected override async Task LoadAsync(GeneralSectionModel model, Proposal proposal)
        {
            var owner = await _userManager.GetUserByIdAsync(proposal.OwnerId);
            var supervisor = await _userManager.GetUserByIdAsync(proposal.SupervisorId);

            model.OwnerName = owner.DisplayName;
            model.SupervisorName = supervisor.DisplayName;
            model.Title = proposal.Title;

            model.ProjectId = proposal.ProjectId;

            await base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(GeneralSectionModel model, Proposal proposal)
        {
            proposal.Title = model.Title;

            return base.StoreAsync(model, proposal);
        }
    }
}