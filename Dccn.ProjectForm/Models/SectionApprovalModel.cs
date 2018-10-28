namespace Dccn.ProjectForm.Models
{
    public class SectionApprovalModel
    {
        public string AuthorityName { get; set; }
        public string AuthorityEmail { get; set; }
        public ApprovalStatusModel Status { get; set; }
    }
}