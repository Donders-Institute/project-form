using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class ExternalPreservationModel
    {
        [Display(Name = "Location", Description = "The location where the external data will be preserved.")]
        public string Location { get; set; }

        [Display(Name = "Reference", Description = "Project number or reference at external location.")]
        public string Reference { get; set; }

        [Display(Name = "Principal Investigator", Description = "The responsible PI at the external location.")]
        public string SupervisorName { get; set; }
    }
}