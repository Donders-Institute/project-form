using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class FundingSectionValidator : AbstractValidator<FundingSectionModel>
    {
        public FundingSectionValidator()
        {
            RuleFor(s => s.ContactEmail).EmailAddress();

            RuleSet("Submit", () =>
            {
                RuleFor(s => s.ContactName).NotEmpty();
                RuleFor(s => s.ContactEmail).NotEmpty();
                RuleFor(s => s.FinancialCode).NotEmpty();
            });
        }
    }
}