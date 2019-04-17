using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class LabValidator : AbstractValidator<LabModel>
    {
        public LabValidator()
        {
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