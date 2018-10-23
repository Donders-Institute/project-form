using Dccn.ProjectForm.Data;

namespace Dccn.ProjectForm.Models
{
    public class SectionApprovalInfo
    {
        public string AuthorityName { get; set; }
        public string AuthorityEmail { get; set; }
        public ApprovalStatus Status { get; set; }
    }
}