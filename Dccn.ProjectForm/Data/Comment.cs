using System;

namespace Dccn.ProjectForm.Data
{
    public class Comment
    {
        public int Id { get; private set; }

        public int ProposalId { get; private set; }

        public string SectionId { get; set; }

        public DateTime CreatedOn { get; private set; }

        public string Content { get; set; }
    }
}