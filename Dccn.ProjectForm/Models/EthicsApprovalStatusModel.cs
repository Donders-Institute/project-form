using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum EthicsApprovalStatusModel
    {
        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Approval pending")]
        Pending
    }
}