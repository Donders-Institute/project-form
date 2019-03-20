using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Dccn.ProjectForm.Authorization
{
    public class FormAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Proposal>
    {
        private readonly IUserManager _userManager;
        private readonly IAuthorityProvider _authorityProvider;

        public FormAuthorizationHandler(IUserManager userManager, IAuthorityProvider authorityProvider)
        {
            _userManager = userManager;
            _authorityProvider = authorityProvider;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Proposal proposal)
        {
            if (_userManager.IsInRole(context.User, Role.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            if (requirement == FormOperation.View)
            {
                if (userId == proposal.OwnerId || proposal.Approvals.Any(approval => _authorityProvider.GetAuthorityIds(proposal, approval.AuthorityRole).Contains(userId)) || _userManager.IsInRole(context.User, Role.Administration))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement == FormOperation.Delete)
            {
                if (proposal.ProjectId != null)
                {
                    return Task.CompletedTask;
                }

                if (userId == proposal.OwnerId || _authorityProvider.GetAdministrationIds(proposal).Contains(userId))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement == FormOperation.Export)
            {
                if (proposal.ProjectId != null)
                {
                    return Task.CompletedTask;
                }

                if (!proposal.Approvals.All(approval => approval.Status == ApprovalStatus.Approved || approval.Status == ApprovalStatus.NotApplicable))
                {
                    return Task.CompletedTask;
                }

                if (_userManager.IsInRole(context.User, Role.Administration))
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
        public static readonly FormOperation Export = new FormOperation {Name = nameof(Export)};
    }
}
