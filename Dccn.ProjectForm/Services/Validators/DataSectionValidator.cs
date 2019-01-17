using System;
using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class DataSectionValidator : AbstractValidator<DataSectionModel>
    {
        public DataSectionValidator(IServiceProvider serviceProvider)
        {
            RuleForEach(s => s.StorageAccessRules)
                .OverrideIndexer((section, rules, rule, index) => $"[{rule.Id}]")
                .NotNull()
                .SetValidator(s => new StorageAccessRuleValidator(serviceProvider, s.StorageAccessRules));

            RuleFor(s => s.Preservation).IsInEnum();

            RuleFor(s => s.ExternalPreservation).NotNull().When(s => s.Preservation == DataPreservationModel.External);

            RuleSet("Submit", () =>
            {
                When(s => s.Preservation == DataPreservationModel.External, () =>
                {
                    RuleFor(s => s.ExternalPreservation.Location).NotEmpty();
                    RuleFor(s => s.ExternalPreservation.Reference).NotEmpty();
                    RuleFor(s => s.ExternalPreservation.SupervisorName).NotEmpty();
                });
            });
        }
    }
}