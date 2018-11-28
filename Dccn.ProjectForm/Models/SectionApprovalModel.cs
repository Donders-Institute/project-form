namespace Dccn.ProjectForm.Models
{
    public class SectionApprovalModel
    {
        public bool IsAutoApproved { get; set; }
        public ApprovalAuthorityRoleModel AuthorityRole { get; set; }
        public string AuthorityName { get; set; }
        public string AuthorityEmail { get; set; }
        public ApprovalStatusModel Status { get; set; }
    }
}