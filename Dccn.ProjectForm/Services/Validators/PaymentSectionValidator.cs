using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class PaymentSectionValidator : AbstractValidator<PaymentSectionModel>
    {
        public PaymentSectionValidator()
        {
            RuleFor(s => s.SubjectCount).GreaterThanOrEqualTo(0);
            RuleFor(s => s.AverageSubjectCost).GreaterThanOrEqualTo(0).LessThan(1000);
            RuleFor(s => s.MaxTotalCost).GreaterThanOrEqualTo(0).LessThan(10_000_000);

            RuleSet("Submit", () =>
            {
                RuleFor(s => s.SubjectCount).NotNull();
                RuleFor(s => s.AverageSubjectCost).NotNull();
                RuleFor(s => s.MaxTotalCost).NotNull();
            });
        }
    }
}