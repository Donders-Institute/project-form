using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Dccn.ProjectForm.Authorization
{
    public class FormAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Proposal>
    {
        private readonly UserManager<ProjectsUser> _userManager;
        private readonly IAuthorityProvider _authorityProvider;

        public FormAuthorizationHandler(UserManager<ProjectsUser> userManager, IAuthorityProvider authorityProvider)
        {
            _userManager = userManager;
            _authorityProvider = authorityProvider;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Proposal proposal)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            if (requirement == FormOperation.View)
            {
                if (userId == proposal.OwnerId || proposal.Approvals.Any(approval => _authorityProvider.GetAuthorityId(proposal, approval.AuthorityRole) == userId))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement == FormOperation.Delete)
            {
                if (userId == proposal.OwnerId)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }

    public sealed class FormOperation : OperationAuthorizationRequirement
    {
        private FormOperation()
        {
        }

        public static readonly FormOperation View = new FormOperation {Name = nameof(View)};
        public static readonly FormOperation Delete = new FormOperation {Name = nameof(Delete)};
    }
}
