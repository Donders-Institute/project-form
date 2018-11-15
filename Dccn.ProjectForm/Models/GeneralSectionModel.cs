using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "General")]
    public class GeneralSectionModel : SectionModelBase
    {
        [Display(Name = "Title.DisplayName", Description = "Title.Description")]
        public string Title { get; set; }

        [BindNever]
        [DisplayName("Project owner")]
        public string OwnerName { get; set; }

        [BindNever]
        [DisplayName("Principal Investigator")]
        public string SupervisorName { get; set; }
    }
}