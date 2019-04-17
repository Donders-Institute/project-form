namespace Dccn.ProjectForm.Email.Models
{
    public class SectionRejected : EmailModelBase
    {
        public override string Subject => "Project proposal: Section rejected";

        public string ApproverName { get; set; }
        public string ProposalTitle { get; set; }
        public string SectionName { get; set; }
        public string PageLink { get; set; }
        public string InvalidatedSections { get; set; }
        public string Reason { get; set; }
    }
}