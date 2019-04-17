using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Dccn.ProjectForm.Authorization
{
    public class FormAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Proposal>
    {
        private readonly IUserManager _userManager;

        public FormAuthorizationHandler(IUserManager userManager)
        {
            _userManager = userManager;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Proposal proposal)
        {
            // Admin can do anything
            if (_userManager.IsInRole(context.User, Role.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            if (requirement == FormOperation.View)
            {
                // Users can view their own proposals; administration/supervisor and authorities can view all proposals
                if (userId == proposal.OwnerId ||
                    _userManager.IsInRole(context.User, Role.Supervisor) ||
                    _userManager.IsInRole(context.User, Role.Authority) ||
                    _userManager.IsInRole(context.User, Role.Administration))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement == FormOperation.Delete)
            {
                // Project ID has been assigned so the proposal is in a read-only state
                if (proposal.ProjectId != null)
                {
                    return Task.CompletedTask;
                }

                // Users can delete their own proposals; administration can delete all proposals
                if (userId == proposal.OwnerId || _userManager.IsInRole(context.User, Role.Administration))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement == FormOperation.Export)
            {
                // Only administration can export
                if (!_userManager.IsInRole(context.User, Role.Administration))
                {
                    return Task.CompletedTask;
                }

                // Project ID has been assigned so the proposal has already been exported
                if (proposal.ProjectId != null)
                {
                    return Task.CompletedTask;
                }

                // Not all sections have been approved
                if (!proposal.Approvals.All(approval => approval.Status == ApprovalStatus.Approved || approval.Status == ApprovalStatus.NotApplicable))
                {
                    return Task.CompletedTask;
                }

                context.Succeed(requirement);
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
