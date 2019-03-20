using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Dccn.ProjectForm.Authorization
{
    public class ApprovalAuthorizationHandler : AuthorizationHandler<ApprovalOperation, Approval>
    {
        private readonly IUserManager _userManager;

        public ApprovalAuthorizationHandler(IUserManager userManager)
        {
            _userManager = userManager;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApprovalOperation requirement, Approval approval)
        {
            if (_userManager.IsInRole(context.User, Role.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (approval.Proposal.ProjectId != null)
            {
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            if (!_userManager.IsInApprovalRole(context.User, approval.AuthorityRole) &&
                (approval.AuthorityRole != ApprovalAuthorityRole.Supervisor ||
                 approval.Proposal.SupervisorId != userId))
            {
                return Task.CompletedTask;
            }

            if (requirement == ApprovalOperation.Approve && CanAuthorityApprove(approval) || requirement == ApprovalOperation.Reject && CanAuthorityReject(approval))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }


        private static bool CanAuthorityApprove(Approval approval)
        {
            return approval.Status == ApprovalStatus.ApprovalPending
                   || approval.Status == ApprovalStatus.Rejected;
        }

        private static bool CanAuthorityReject(Approval approval)
        {
            return approval.Status == ApprovalStatus.ApprovalPending
                   || approval.Status == ApprovalStatus.Approved;
        }
    }

    public sealed class ApprovalOperation : OperationAuthorizationRequirement
    {
        private ApprovalOperation()
        {
        }

        public static readonly ApprovalOperation Approve = new ApprovalOperation {Name = nameof(Approve)};
        public static readonly ApprovalOperation Reject = new ApprovalOperation {Name = nameof(Reject)};
    }
}
