using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class ModalityModel
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public bool IsMri { get; set; }
        public int SessionStorageQuota { get; set; }
        public bool Hidden { get; set; }
    }
}