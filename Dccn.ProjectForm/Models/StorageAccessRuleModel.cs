using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class StorageAccessRuleModel
    {
        public UserModel User { get; set; }
        public StorageAccessRoleModel Role { get; set; }
        [BindNever] public bool CanEdit { get; set; }
        [BindNever] public bool CanRemove { get; set; }
    }
}