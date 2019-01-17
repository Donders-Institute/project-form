using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum EthicsApprovalStatusModel
    {
        [Display(Name = "Form.Ethics.Approved.Label", Description = "Form.Ethics.Approved.Description")]
        Approved,

        [Display(Name = "Form.Ethics.Pending.Label", Description = "Form.Ethics.Pending.Description")]
        Pending
    }
}