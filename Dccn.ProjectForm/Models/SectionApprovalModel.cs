namespace Dccn.ProjectForm.Models
{
    public class SectionApprovalModel
    {
        public bool IsAutoApproved { get; set; }
        public bool IsSelfApproved { get; set; }
        public ApprovalAuthorityRoleModel AuthorityRole { get; set; }
        public string AuthorityName { get; set; }
        public string AuthorityEmail { get; set; }
        public ApprovalStatusModel Status { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
    }
}