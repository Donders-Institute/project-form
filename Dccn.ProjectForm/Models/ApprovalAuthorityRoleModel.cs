using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ApprovalAuthorityRoleModel
    {
        [Display(Name = "Ethical approval")]
        Ethics,

        [Display(Name = "Lab coordinator (MRI)")]
        LabMri,

        [Display(Name = "Lab coordinator (non-MRI)")]
        LabOther,

        [Display(Name = "Privacy officer")]
        Privacy,

        [Display(Name = "Funding")] // TODO: better name?
        Funding,

        [Display(Name = "Principal investigator")]
        Supervisor,

        [Display(Name = "Director")]
        Director,

        [Display(Name = "Administration")]
        Administration
    }
}