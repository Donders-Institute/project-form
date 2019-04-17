namespace Dccn.ProjectForm.Email.Models
{
    public class SectionApproved : EmailModelBase
    {
        public override string Subject => "Project proposal: Section approved";

        public string ApproverName { get; set; }
        public string Remarks { get; set; }
        public string ProposalTitle { get; set; }
        public string SectionName { get; set; }
        public string PageLink { get; set; }
    }
}