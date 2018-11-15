using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Ethical approval")]
    public class EthicsSectionModel : SectionModelBase
    {
        [Display(Name = "Approved")]
        public EthicsApprovalStatusModel Status { get; set; }

        [Display(Name = "Ethics committee approval number")]
        public EthicsApprovalOptionModel ApprovalCode { get; set; }

        public string CustomCode { get; set; }

        [Display(Name = "Correspondence number")]
        public string CorrespondenceNumber { get; set; }
    }
}