using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ApprovalAuthorityRoleModel
    {
        [Display(Name = "Supervisor")]
        Supervisor,

        [Display(Name = "Ethical approval")]
        Ethics,

        [Display(Name = "Funding")]
        Funding,

        [Display(Name = "Lab coordinator (MRI)")]
        LabMri,

        [Display(Name = "Lab coordinator (other)")]
        LabOther,

        [Display(Name = "Data dummy")]
        DataManager,

        [Display(Name = "Privacy officer")]
        Privacy,

        [Display(Name = "Payment dummy")]
        Payment,

        [Display(Name = "Director")]
        Director
    }
}