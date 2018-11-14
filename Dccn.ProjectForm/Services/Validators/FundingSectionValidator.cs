using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class FundingSectionValidator : AbstractValidator<FundingSectionModel>
    {
        public FundingSectionValidator()
        {
            RuleFor(s => s.ContactName).NotEmpty();
            RuleFor(s => s.ContactEmail).NotEmpty().EmailAddress();
            RuleFor(s => s.FinancialCode).NotEmpty();

            RuleSet("Submit", () =>
            {
                RuleFor(s => s.ContactName).NotNull();
                RuleFor(s => s.ContactEmail).NotNull();
                RuleFor(s => s.FinancialCode).NotNull();
            });
        }
    }
}