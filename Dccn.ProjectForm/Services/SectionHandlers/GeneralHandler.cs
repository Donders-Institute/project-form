using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class GeneralHandler : FormSectionHandlerBase<General>
    {
        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => Enumerable.Empty<ApprovalAuthorityRole>();

        public GeneralHandler(IAuthorityProvider authorityProvider): base(authorityProvider, m => m.General)
        {
        }

        protected override Task LoadAsync(General model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.OwnerName = owner.DisplayName;
            model.PrincipalInvestigatorName = supervisor.DisplayName;
            model.Title = proposal.Title;

            return base.LoadAsync(model, proposal, owner, supervisor);
        }

        protected override Task StoreAsync(General model, Proposal proposal)
        {
            proposal.Title = model.Title;

            return base.StoreAsync(model, proposal);
        }
    }
}