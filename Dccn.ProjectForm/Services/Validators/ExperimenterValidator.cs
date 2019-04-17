using System;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.Validators
{
    public class ExperimenterValidator : UserValidator<ExperimenterModel>
    {
        public ExperimenterValidator(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}