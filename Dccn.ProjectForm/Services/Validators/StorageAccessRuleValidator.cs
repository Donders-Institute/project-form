using System;
using System.Collections.Generic;
using Dccn.ProjectForm.Models;
using FluentValidation;

namespace Dccn.ProjectForm.Services.Validators
{
    public class StorageAccessRuleValidator : UserValidator<StorageAccessRuleModel>
    {
        public StorageAccessRuleValidator(IServiceProvider serviceProvider, IEnumerable<StorageAccessRuleModel> collection) : base(serviceProvider, collection)
        {
            RuleFor(r => r.Role).IsInEnum();
        }
    }
}