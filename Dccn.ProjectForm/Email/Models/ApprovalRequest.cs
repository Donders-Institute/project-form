namespace Dccn.ProjectForm.Email.Models
{
    public class ApprovalRequest : EmailModelBase
    {
        public override string Subject => "Request of approval";
        public string Applicant { get; set; }
        public string ProposalTitle { get; set; }
        public string SectionName { get; set; }
    }
}