using System;
using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class ExperimentSectionValidator : AbstractValidator<ExperimentSectionModel>
    {
        public ExperimentSectionValidator(IServiceProvider serviceProvider)
        {
            RuleFor(s => s.StartDate);
            RuleFor(s => s.EndDate);

            RuleFor(s => s.StorageQuota).IsInEnum();
            When(s => s.StorageQuota == StorageQuotaModel.Standard, () =>
            {
                RuleFor(s => s.CustomQuotaAmount).Null();
                RuleFor(s => s.CustomQuotaMotivation).Null();
            });
            When(s => s.StorageQuota == StorageQuotaModel.Custom, () =>
            {
                RuleFor(s => s.CustomQuotaAmount).GreaterThanOrEqualTo(0);
                RuleFor(s => s.CustomQuotaMotivation);
            });

            RuleForEach(s => s.Labs)
                .NotNull()
                .SetValidator(new LabValidator(serviceProvider));

            RuleForEach(s => s.Experimenters)
                .NotNull()
                .SetValidator(s => new ExperimenterValidator(serviceProvider, s.Experimenters));

            RuleSet("Submit", () =>
            {
                RuleFor(s => s.StartDate).NotNull();
                RuleFor(s => s.EndDate).NotNull();
                RuleFor(s => s.EndDate).GreaterThanOrEqualTo(s => s.StartDate);

                When(s => s.StorageQuota == StorageQuotaModel.Custom, () =>
                {
                    RuleFor(s => s.CustomQuotaAmount).NotNull();
                    RuleFor(s => s.CustomQuotaMotivation)
                        .NotNull()
                        .NotEmpty();
                });

                RuleForEach(s => s.Experimenters).NotEmpty();
            });
        }
    }
}