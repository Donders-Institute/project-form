using System;
using System.Collections.Generic;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class PrivacySectionHandler : FormSectionHandlerBase<PrivacySectionModel>
    {
        public PrivacySectionHandler(IServiceProvider serviceProvider)
            : base(serviceProvider, m => m.Privacy)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new [] {ApprovalAuthorityRole.Privacy};

        // TODO: LoadAsync
        // TODO: StoreAsync
    }
}