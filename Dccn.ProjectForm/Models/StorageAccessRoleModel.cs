using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum StorageAccessRoleModel
    {
        [Display(Name = "Manager")]
        Manager,

        [Display(Name = "Contributor")]
        Contributor,

        [Display(Name = "Viewer")]
        Viewer
    }
}