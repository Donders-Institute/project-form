using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class RetractSectionModel
    {
        [Required]
        public string Section { get; set; }
    }
}