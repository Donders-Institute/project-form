using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class PaymentSectionHandler : FormSectionHandlerBase<PaymentSectionModel>
    {
        public PaymentSectionHandler(IServiceProvider serviceProvider)
            : base(serviceProvider, m => m.Payment)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Payment};

        protected override Task LoadAsync(PaymentSectionModel model, Proposal proposal)
        {
            model.SubjectCount = proposal.PaymentSubjectCount;
            model.AverageSubjectCost = proposal.PaymentAverageSubjectCost;
            model.MaxTotalCost = proposal.PaymentMaxTotalCost;

            return base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(PaymentSectionModel model, Proposal proposal)
        {
            proposal.PaymentSubjectCount = model.SubjectCount;
            proposal.PaymentAverageSubjectCost = model.AverageSubjectCost;
            proposal.PaymentMaxTotalCost = model.MaxTotalCost;

            return base.StoreAsync(model, proposal);
        }

        public override bool SectionEquals(Proposal x, Proposal y) =>
            x.PaymentSubjectCount == y.PaymentSubjectCount
            && x.PaymentAverageSubjectCost == y.PaymentAverageSubjectCost
            && x.PaymentMaxTotalCost == y.PaymentMaxTotalCost
            && base.SectionEquals(x, y);
    }
}