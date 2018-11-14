using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class ModalityModel
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
    }
}