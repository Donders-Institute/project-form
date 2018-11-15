using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class EthicsSectionValidator : AbstractValidator<EthicsSectionModel>
    {
        public EthicsSectionValidator()
        {
            RuleFor(s => s.ApprovalCode).IsInEnum().When(s => s.Status == EthicsApprovalStatusModel.Approved);

            RuleSet("Submit", () =>
            {
                When(s => s.Status == EthicsApprovalStatusModel.Approved, () =>
                {
                    RuleFor(s => s.CustomCode).NotEmpty().When(s => s.ApprovalCode == EthicsApprovalOptionModel.Other);
                });
                When(s => s.Status == EthicsApprovalStatusModel.Pending, () =>
                {
                    RuleFor(s => s.CorrespondenceNumber).NotEmpty();
                });
            });
        }
    }
}