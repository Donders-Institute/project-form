using System.ComponentModel.DataAnnotations;
using Dccn.ProjectForm.Data;

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

        [Display(Name = "Approval role")]
        public ApprovalAuthorityRole ApprovalRole { get; set; }
    }
}