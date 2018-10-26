using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Dccn.ProjectForm.Authorization
{
    public class FormSectionAuthorizationHandler : AuthorizationHandler<FormSectionOperation, Proposal>
    {
        private readonly IUserManager _userManager;
        private readonly IAuthorityProvider _authorityProvider;

        public FormSectionAuthorizationHandler(IUserManager userManager, IAuthorityProvider authorityProvider)
        {
            _userManager = userManager;
            _authorityProvider = authorityProvider;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FormSectionOperation requirement, Proposal proposal)
        {
            if (_userManager.IsInRole(context.User, Role.Administrator))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            var sectionHandler = requirement.SectionHandler;
            var approvals = sectionHandler.GetAssociatedApprovals(proposal);
            switch (requirement.Operation)
            {
                case FormSectionOperation.Type.Edit when (userId == proposal.OwnerId || userId == proposal.SupervisorId) && approvals.All(CanOwnerEdit):
                    context.Succeed(requirement);
                    break;
                case FormSectionOperation.Type.Edit:
                {
                    var approval = approvals.SingleOrDefault(a => _authorityProvider.GetAuthorityId(proposal, a.AuthorityRole) == userId);
                    if (approval != null && CanAuthorityEdit(approval))
                    {
                        context.Succeed(requirement);
                    }

                    break;
                }
                case FormSectionOperation.Type.Submit when userId == proposal.OwnerId && approvals.All(CanOwnerSubmit):
                    context.Succeed(requirement);
                    break;
                case FormSectionOperation.Type.Approve:
                {
                    var approval = approvals.SingleOrDefault(a => _authorityProvider.GetAuthorityId(proposal, a.AuthorityRole) == userId);
                    if (approval != null && CanAuthorityApprove(approval))
                    {
                        context.Succeed(requirement);
                    }

                    break;
                }
            }

            return Task.CompletedTask;
        }

        private static bool CanOwnerEdit(Approval approval)
        {
            return approval.Status == ApprovalStatus.NotSubmitted
                   || approval.Status == ApprovalStatus.NotApplicable
                   || approval.Status == ApprovalStatus.Rejected;
        }

        private static bool CanOwnerSubmit(Approval approval)
        {
            return approval.Status == ApprovalStatus.NotSubmitted
                   || approval.Status == ApprovalStatus.NotApplicable
                   || approval.Status == ApprovalStatus.Rejected;
        }

        private static bool CanAuthorityEdit(Approval approval)
        {
            return approval.Status == ApprovalStatus.ApprovalPending;
        }

        private static bool CanAuthorityApprove(Approval approval)
        {
            return approval.Status == ApprovalStatus.ApprovalPending;
        }
    }

    public sealed class FormSectionOperation : OperationAuthorizationRequirement
    {
        public enum Type
        {
            Edit,
            Approve,
            Submit
        }

        private FormSectionOperation(Type operation, IFormSectionHandler sectionHandler)
        {
            Operation = operation;
            SectionHandler = sectionHandler;
            Name = $"{operation.ToString()} {sectionHandler}";
        }

        public static FormSectionOperation Edit(IFormSectionHandler sectionHandler)
        {
            return new FormSectionOperation(Type.Edit, sectionHandler);
        }

        public static FormSectionOperation Approve(IFormSectionHandler sectionHandler)
        {
            return new FormSectionOperation(Type.Approve, sectionHandler);
        }

        public static FormSectionOperation Submit(IFormSectionHandler sectionHandler)
        {
            return new FormSectionOperation(Type.Submit, sectionHandler);
        }

        public Type Operation { get; }
        public IFormSectionHandler SectionHandler { get; }
    }
}
