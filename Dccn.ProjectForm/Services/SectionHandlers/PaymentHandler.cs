using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class PaymentHandler : FormSectionHandlerBase<Payment>
    {
        public PaymentHandler(IAuthorityProvider authorityProvider): base(authorityProvider, m => m.Payment)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => Enumerable.Empty<ApprovalAuthorityRole>();

        protected override Task LoadAsync(Payment model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.SubjectCount = proposal.PaymentSubjectCount;
            model.AverageSubjectCost = proposal.PaymentAverageSubjectCost;
            model.MaxTotalCost = proposal.PaymentMaxTotalCost;

            return base.LoadAsync(model, proposal, owner, supervisor);
        }

        protected override Task StoreAsync(Payment model, Proposal proposal)
        {
            proposal.PaymentSubjectCount = model.SubjectCount;
            proposal.PaymentAverageSubjectCost = model.AverageSubjectCost;
            proposal.PaymentMaxTotalCost = model.MaxTotalCost;

            return base.StoreAsync(model, proposal);
        }
    }
}