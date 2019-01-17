using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Ethics.Title", Description = "Form.Ethics.Description")]
    public class EthicsSectionModel : SectionModelBase
    {
        public EthicsApprovalStatusModel Status { get; set; }

        [Display(Name = "Form.Ethics.Approved.ApprovalCode.Label", Description = "Form.Ethics.Approved.ApprovalCode.Description")]
        public EthicsApprovalOptionModel ApprovalCode { get; set; }

        public string CustomCode { get; set; }

        [Display(Name = "Form.Ethics.Pending.CorrespondenceNumber.Label", Description = "Form.Ethics.Pending.CorrespondenceNumber.Description")]
        public string CorrespondenceNumber { get; set; }
    }
}