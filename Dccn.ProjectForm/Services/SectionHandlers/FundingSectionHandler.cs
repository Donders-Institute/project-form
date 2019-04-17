using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class FundingSectionHandler : FormSectionHandlerBase<FundingSectionModel>
    {
        public FundingSectionHandler(IServiceProvider serviceProvider)
            : base(serviceProvider, m => m.Funding)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Funding};

        protected override Task LoadAsync(FundingSectionModel model, Proposal proposal)
        {
            model.ContactName = proposal.FundingContactName;
            model.ContactEmail = proposal.FundingContactEmail;
            model.FinancialCode = proposal.FinancialCode;

            return base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(FundingSectionModel model, Proposal proposal)
        {
            proposal.FundingContactName = model.ContactName;
            proposal.FundingContactEmail = model.ContactEmail;
            proposal.FinancialCode = model.FinancialCode;

            return base.StoreAsync(model, proposal);
        }

        public override bool SectionEquals(Proposal x, Proposal y) =>
            x.FundingContactName == y.FundingContactName
            && x.FundingContactEmail == y.FundingContactEmail
            && x.FinancialCode == y.FinancialCode
            && base.SectionEquals(x, y);
    }
}