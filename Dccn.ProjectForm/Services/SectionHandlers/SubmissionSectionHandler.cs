using System;
using System.Collections.Generic;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class SubmissionSectionHandler : FormSectionHandlerBase<SubmissionSectionModel>
    {
        public SubmissionSectionHandler(IServiceProvider serviceProvider) : base(serviceProvider, m => m.Submission)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new[] {ApprovalAuthorityRole.Director};
    }
}