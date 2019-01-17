using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.General.Title")]
    public class GeneralSectionModel : SectionModelBase
    {
        [Display(Name = "Form.General.PpmTitle.Label", Description = "Form.General.PpmTitle.Description")]
        public string Title { get; set; }

        [BindNever]
        [Display(Name = "Form.General.OwnerName.Label", Description = "Form.General.OwnerName.Description")]
        public string OwnerName { get; set; }

        [BindNever]
        [Display(Name = "Form.General.SupervisorName.Label", Description = "Form.General.SupervisorName.Description")]
        public string SupervisorName { get; set; }
    }
}