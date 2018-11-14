using System;
using Dccn.ProjectForm.Models;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services.Validators
{
    public class LabValidator : AbstractValidator<LabModel>
    {
        public LabValidator(IServiceProvider serviceProvider)
        {
            var modalityProvider = serviceProvider.GetRequiredService<IModalityProvider>();

            RuleFor(l => l.Modality.Id)
                .NotNull()
                .NotEmpty()
                .Must(id => modalityProvider.Exists(id));

            RuleFor(l => l.SubjectCount).GreaterThanOrEqualTo(0);
            RuleFor(l => l.ExtraSubjectCount).GreaterThanOrEqualTo(0);
            RuleFor(l => l.SessionCount).GreaterThanOrEqualTo(0);
            RuleFor(l => l.SessionDurationMinutes).GreaterThanOrEqualTo(0);

            RuleSet("Submit", () =>
            {
                RuleFor(l => l.SubjectCount).NotNull();
                RuleFor(l => l.ExtraSubjectCount).NotNull();
                RuleFor(l => l.SessionCount).NotNull();
                RuleFor(l => l.SessionDurationMinutes).NotNull();
            });
        }
    }
}