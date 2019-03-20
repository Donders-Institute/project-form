namespace Dccn.ProjectForm.Email.Models
{
    public class ApprovalRequest : EmailModelBase
    {
        public override string Subject => "Project proposal: Proposal approved";

        public string ApplicantName { get; set; }
        public string ProposalTitle { get; set; }
        public string SectionName { get; set; }
        public string PageLink { get; set; }
    }
}