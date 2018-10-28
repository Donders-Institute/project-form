using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "General")]
    public class GeneralSectionModel : SectionModelBase
    {
        [Display(Name = "Title.DisplayName", Description = "Title.Description")]
        [Required(ErrorMessage = "Title.Required.ErrorMessage")]
        [MinLength(10, ErrorMessage = "Title.MinLength.ErrorMessage")]
        public string Title { get; set; }

        [BindNever]
        [DisplayName("Project owner")]
        public string OwnerName { get; set; }

        [BindNever]
        [DisplayName("Principal Investigator")]
        public string SupervisorName { get; set; }
    }
}