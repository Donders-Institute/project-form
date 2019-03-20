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
                RuleFor(s => s.CustomStorageQuota).Null();
                RuleFor(s => s.CustomStorageQuotaMotivation).Null();
            });
            When(s => s.StorageQuota == StorageQuotaModel.Custom, () =>
            {
                RuleFor(s => s.CustomStorageQuota).GreaterThanOrEqualTo(0);
                RuleFor(s => s.CustomStorageQuotaMotivation);
            });

            RuleForEach(s => s.Labs)
                .OverrideIndexer((section, labs, lab, index) => $"[{lab.Id}]")
                .NotNull()
                .SetValidator(new LabValidator(serviceProvider));

            RuleForEach(s => s.Experimenters)
                .OverrideIndexer((section, experimenters, experimenter, index) => $"[{experimenter.Id}]")
                .NotNull()
                .SetValidator(s => new ExperimenterValidator(serviceProvider, s.Experimenters));

            RuleSet("Submit", () =>
            {
                RuleFor(s => s.StartDate).NotNull();
                RuleFor(s => s.EndDate).NotNull();
                RuleFor(s => s.EndDate).GreaterThanOrEqualTo(s => s.StartDate);

                When(s => s.StorageQuota == StorageQuotaModel.Custom, () =>
                {
                    RuleFor(s => s.CustomStorageQuota).NotNull();
                    RuleFor(s => s.CustomStorageQuotaMotivation)
                        .NotNull()
                        .NotEmpty();
                });

                RuleForEach(s => s.Experimenters).NotEmpty();
            });
        }
    }
}