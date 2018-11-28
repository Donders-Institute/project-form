using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ApprovalAuthorityRoleModel
    {
        [Display(Name = "Principal investigator")]
        Supervisor,

        [Display(Name = "Ethical approval")]
        Ethics,

        [Display(Name = "Funding")]
        Funding,

        [Display(Name = "Lab coordinator (MRI)")]
        LabMri,

        [Display(Name = "Lab coordinator (non-MRI)")]
        LabOther,

        [Display(Name = "Data dummy")]
        DataManager,

        [Display(Name = "Privacy officer")]
        Privacy,

        [Display(Name = "Payment dummy")]
        Payment,

        [Display(Name = "Director")]
        Director,

        [Display(Name = "Administration")]
        Administration
    }
}