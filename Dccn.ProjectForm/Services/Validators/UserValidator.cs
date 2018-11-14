using System;
using System.Collections.Generic;
using System.Linq;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Models;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services.Validators
{
    public abstract class UserValidator<T> : AbstractValidator<T> where T : UserModel
    {
        protected UserValidator(IServiceProvider serviceProvider, IEnumerable<T> collection)
        {
            var userManager = serviceProvider.GetRequiredService<IUserManager>();

            RuleFor(u => u.Id)
                .NotNull()
                .NotEmpty()
                .MustAsync((id, _) => userManager.UserExistsAsync(id))
                .Must(id => collection.Count(u => u.Id == id) == 1);
        }
    }
}