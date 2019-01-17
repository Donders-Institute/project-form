using System;

namespace Dccn.ProjectForm.Email.Models
{
    public class ApprovalRequestModel : EmailModelBase
    {
        public override string TemplateName => "ApprovalRequest";
        public override string Subject => "Request of approval";
        public string ApproverName { get; set; }
        public string ApplicantName { get; set; }
        public string ProposalTitle { get; set; }
        public string SectionName { get; set; }
        public string PageLink { get; set; }
    }
}