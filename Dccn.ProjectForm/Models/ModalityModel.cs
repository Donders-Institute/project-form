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
        public int SessionStorageQuota { get; set; }
    }
}