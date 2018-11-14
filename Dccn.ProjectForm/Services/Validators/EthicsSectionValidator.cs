using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class EthicsSectionValidator : AbstractValidator<EthicsSectionModel>
    {
        public EthicsSectionValidator()
        {
            When(s => s.Approved, () =>
            {
                RuleFor(s => s.CorrespondenceNumber).NotEmpty();
            });
            Unless(s => s.Approved, () =>
            {

            });

            RuleSet("Submit", () =>
            {

            });
        }
    }
}