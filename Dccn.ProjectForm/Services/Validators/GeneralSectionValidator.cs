﻿using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class GeneralSectionValidator : AbstractValidator<GeneralSectionModel>
    {
        public GeneralSectionValidator()
        {
            RuleFor(s => s.Title).NotEmpty().MinimumLength(10);
        }
    }
}