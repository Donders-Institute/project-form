using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class ApprovalModel
    {
        public int ProposalId { get; set; }

        public string SectionId { get; set; }

        [Display(Name = "Section")]
        public string SectionName { get; set; }

        [Display(Name = "Title")]
        public string ProposalTitle { get; set; }

        [Display(Name = "Project owner")]
        public string ProposalOwnerName { get; set; }

        [Display(Name = "Role")]
        public ApprovalAuthorityRoleModel Role { get; set; }

        [Display(Name = "Status")]
        public ApprovalStatusModel Status { get; set; }

        public ApprovalRoleTypeModel RoleType { get; set; }
    }
}