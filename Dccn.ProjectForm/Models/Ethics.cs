using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Ethical approval")]
    public class Ethics : SectionModelBase
    {
        public enum ApprovalType
        {
            Blanket,
            Children,
            Other
        }

        [Display(Name = "Approved")]
        public bool Approved { get; set; }

        [Display(Name = "Ethics committee approval number")]
        public ApprovalType ApprovalCode { get; set; }

        public string CustomCode { get; set; }

        [Display(Name = "Correspondence number")]
        public string CorrespondenceNumber { get; set; }
    }
}