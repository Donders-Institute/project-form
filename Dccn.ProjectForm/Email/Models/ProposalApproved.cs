namespace Dccn.ProjectForm.Email.Models
{
    public class ProposalApproved : EmailModelBase
    {
        public override string Subject => "Project proposal: Ready for submission";

        public string ApplicantName { get; set; }
        public string ProposalTitle { get; set; }
        public string PageLink { get; set; }
    }
}