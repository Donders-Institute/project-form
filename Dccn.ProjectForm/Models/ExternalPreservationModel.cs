using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class ExternalPreservationModel
    {
        [Display(Name = "Form.Data.Preservation.External.Location.Label", Description = "Form.Data.Preservation.External.Location.Description")]
        public string Location { get; set; }

        [Display(Name = "Form.Data.Preservation.External.Reference.Label", Description = "Form.Data.Preservation.External.Reference.Description")]
        public string Reference { get; set; }

        [Display(Name = "Form.Data.Preservation.External.PrincipalInvestigator.Label", Description = "Form.Data.Preservation.External.PrincipalInvestigator.Description")]
        public string SupervisorName { get; set; }
    }
}