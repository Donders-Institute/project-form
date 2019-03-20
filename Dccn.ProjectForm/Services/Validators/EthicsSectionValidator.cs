using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class EthicsSectionValidator : AbstractValidator<EthicsSectionModel>
    {
        public EthicsSectionValidator()
        {
            // TODO: maybe check for validity? not really necessary
            // RuleFor(s => s.ApprovalCode).When(s => s.Status == EthicsApprovalStatusModel.Approved);
            // RuleFor(s => s.StandardCode).NotEmpty();

            RuleSet("Submit", () =>
            {
                When(s => s.Status == EthicsApprovalStatusModel.Approved, () =>
                {
                    RuleFor(s => s.Code).NotEmpty();
                });
                When(s => s.Status == EthicsApprovalStatusModel.Pending, () =>
                {
                    RuleFor(s => s.CorrespondenceNumber).NotEmpty();
                });
            });
        }
    }
}