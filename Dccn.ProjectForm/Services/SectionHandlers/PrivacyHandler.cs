using System.Collections.Generic;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class PrivacyHandler : FormSectionHandlerBase<Privacy>
    {
        public PrivacyHandler(IAuthorityProvider authorityProvider) : base(authorityProvider)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new [] {ApprovalAuthorityRole.Privacy};

        // TODO: LoadAsync
        // TODO: StoreAsync
    }
}