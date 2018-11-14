using System;
using System.Collections.Generic;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.Validators
{
    public class ExperimenterValidator : UserValidator<ExperimenterModel>
    {
        public ExperimenterValidator(IServiceProvider serviceProvider, IEnumerable<ExperimenterModel> collection) : base(serviceProvider, collection)
        {
        }
    }
}