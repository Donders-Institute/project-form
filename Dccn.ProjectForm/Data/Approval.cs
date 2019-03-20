namespace Dccn.ProjectForm.Data
{
    public class Approval
    {
        public int ProposalId { get; private set; }
        public Proposal Proposal { get; private set; }

        public ApprovalAuthorityRole AuthorityRole { get; set; }
        public ApprovalStatus Status { get; set; }
        public string ValidatedBy { get; set; }
    }

    public enum ApprovalAuthorityRole
    {
        Supervisor,
        Ethics,
        Funding,
        LabMri,
        LabOther,
        DataManager,
        Privacy,
        Payment,
        Director
    }

    public enum ApprovalStatus
    {
        NotSubmitted,
        NotApplicable,
        ApprovalPending,
        Approved,
        Rejected
    }
}