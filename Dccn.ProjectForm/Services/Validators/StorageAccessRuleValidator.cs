using System;
using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class StorageAccessRuleValidator : UserValidator<StorageAccessRuleModel>
    {
        public StorageAccessRuleValidator(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RuleFor(r => r.Role).IsInEnum();
        }
    }
}