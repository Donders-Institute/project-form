using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dccn.ProjectForm.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Models
{
    public class ModalityModel : IValidatableObject
    {
        public string Id { get; set; }

        [BindNever]
        public string DisplayName { get; set; }

        [BindNever]
        public bool IsMri { get; set; }

        [BindNever]
        public decimal FixedStorage { get; set; }

        [BindNever]
        public decimal SessionStorage { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var provider = validationContext.GetService<IModalityProvider>();
            if (!provider.Exists(Id))
            {
                yield return new ValidationResult("Unknown lab modality.");
            }
        }
    }
}