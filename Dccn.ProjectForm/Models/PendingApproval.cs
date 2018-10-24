using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class PendingApproval
    {
        public int ProposalId { get; set; }

        public string SectionId { get; set; }

        [Display(Name = "Title")]
        public string ProposalTitle { get; set; }

        [Display(Name = "Project owner")]
        public string ProposalOwnerName { get; set; }

        [Display(Name = "Approval type")]
        public string ApprovalType { get; set; }
    }
}