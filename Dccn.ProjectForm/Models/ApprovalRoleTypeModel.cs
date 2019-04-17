using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ApprovalRoleTypeModel
    {
        [Display(Name = "Primary")]
        Primary,

        [Display(Name = "Secondary")]
        Secondary
    }
}