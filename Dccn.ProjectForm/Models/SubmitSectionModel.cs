using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class SubmitSectionModel
    {
        [Required]
        public string Section { get; set; }
    }
}