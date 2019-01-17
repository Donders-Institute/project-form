using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum StorageAccessRoleModel
    {
        [Display(Name = "Form.Data.ProjectStorage.Manager")]
        Manager,

        [Display(Name = "Form.Data.ProjectStorage.Contributor")]
        Contributor,

        [Display(Name = "Form.Data.ProjectStorage.Viewer")]
        Viewer
    }
}