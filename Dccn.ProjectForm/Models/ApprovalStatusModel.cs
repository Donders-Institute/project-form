using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ApprovalStatusModel
    {
        [Display(Name = "Not submitted")]
        NotSubmitted,

        [Display(Name = "Not applicable")]
        NotApplicable,

        [Display(Name = "Approval pending")]
        ApprovalPending,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected
    }
}