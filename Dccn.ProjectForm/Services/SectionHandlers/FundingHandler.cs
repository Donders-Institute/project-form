using System.Collections.Generic;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class FundingHandler : FormSectionHandlerBase<Funding>
    {
        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Funding};

        public FundingHandler(IAuthorityProvider authorityProvider): base(authorityProvider)
        {
        }

        public override Task LoadAsync(Funding model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.ContactName = proposal.FundingContactName;
            model.ContactEmail = proposal.FundingContactEmail;
            model.FinancialCode = proposal.FinancialCode;

            return base.LoadAsync(model, proposal, owner, supervisor);
        }

        public override Task StoreAsync(Funding model, Proposal proposal)
        {
            proposal.FundingContactName = model.ContactName;
            proposal.FundingContactEmail = model.ContactEmail;
            proposal.FinancialCode = model.FinancialCode;

            return base.StoreAsync(model, proposal);
        }
    }
}