namespace Dccn.ProjectForm.Data
{
    public class Approval
    {
        public int ProposalId { get; private set; }

        public ApprovalAuthorityRole AuthorityRole { get; set; }
        public ApprovalStatus Status { get; set; }
        public string ApprovedBy { get; set; }
    }

    public enum ApprovalAuthorityRole
    {
        Ethics,
        LabMri,
        LabOther,
        Privacy,
        Funding,
        Supervisor,
        Director,
        Administration
    }

    public enum ApprovalStatus
    {
        NotSubmitted,
        ApprovalPending,
        Approved,
        Rejected
    }
}