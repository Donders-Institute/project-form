using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ApprovalStatusModel
    {
        [Display(Name = "Not submitted")]
        NotSubmitted,

        [Display(Name = "N/A")]
        NotApplicable,

        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected
    }
}