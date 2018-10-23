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

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Proposal resource)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            if (requirement == FormOperations.Edit || requirement == FormOperations.View)
            {
                if (userId == resource.OwnerId ||
                    resource.Approvals.Any(approval => _authorityProvider.GetAuthorityId(resource, approval.AuthorityRole) == userId))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement == FormOperations.Delete)
            {
                if (userId == resource.OwnerId)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }

    public static class FormOperations
    {
        public static readonly OperationAuthorizationRequirement View = new OperationAuthorizationRequirement {Name = nameof(View)};
        public static readonly OperationAuthorizationRequirement Edit = new OperationAuthorizationRequirement {Name = nameof(Edit)};
        public static readonly OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement {Name = nameof(Delete)};
    }
}
