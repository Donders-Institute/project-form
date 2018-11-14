using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Dccn.ProjectForm.Authorization
{
    public class FormSectionAuthorizationHandler : AuthorizationHandler<FormSectionOperation, Proposal>
    {
        private readonly IUserManager _userManager;
        private readonly IAuthorityProvider _authorityProvider;
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;

        public FormSectionAuthorizationHandler(IUserManager userManager, IAuthorityProvider authorityProvider, IEnumerable<IFormSectionHandler> sectionHandlers)
        {
            _userManager = userManager;
            _authorityProvider = authorityProvider;
            _sectionHandlers = sectionHandlers;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FormSectionOperation requirement, Proposal proposal)
        {
            if (_userManager.IsInRole(context.User, Role.Administrator))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            var sectionHandler =  _sectionHandlers.Single(h => h.ModelType == requirement.SectionType);
            var approvals =  sectionHandler.GetAssociatedApprovals(proposal);
            switch (requirement.Operation)
            {
                case FormSectionOperation.OperationType.Edit when (userId == proposal.OwnerId || userId == proposal.SupervisorId) && approvals.All(CanOwnerEdit):
                    context.Succeed(requirement);
                    break;
                case FormSectionOperation.OperationType.Edit:
                {
                    var approval = approvals.SingleOrDefault(a => _authorityProvider.GetAuthorityId(proposal, a.AuthorityRole) == userId);
                    if (approval != null && CanAuthorityEdit(approval))
                    {
                        context.Succeed(requirement);
                    }

                    break;
                }
                case FormSectionOperation.OperationType.Submit when userId == proposal.OwnerId && approvals.All(CanOwnerSubmit):
                    context.Succeed(requirement);
                    break;
                case FormSectionOperation.OperationType.Approve:
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
        public enum OperationType
        {
            Edit,
            Approve,
            Submit
        }

        private FormSectionOperation(OperationType operation, Type sectionType)
        {
            if (!typeof(ISectionModel).IsAssignableFrom(sectionType))
            {
                throw new ArgumentException("Illegal type.", nameof(sectionType));
            }

            Operation = operation;
            SectionType = sectionType;
            Name = $"{operation.ToString()} {sectionType}";
        }

        public static FormSectionOperation Edit(Type sectionType)
        {
            return new FormSectionOperation(OperationType.Edit, sectionType);
        }

        public static FormSectionOperation Approve(Type sectionType)
        {
            return new FormSectionOperation(OperationType.Approve, sectionType);
        }

        public static FormSectionOperation Submit(Type sectionType)
        {
            return new FormSectionOperation(OperationType.Submit, sectionType);
        }

        public static FormSectionOperation Edit<TSection>() where TSection : ISectionModel
        {
            return new FormSectionOperation(OperationType.Edit, typeof(TSection));
        }

        public static FormSectionOperation Approve<TSection>() where TSection : ISectionModel
        {
            return new FormSectionOperation(OperationType.Approve, typeof(TSection));
        }

        public static FormSectionOperation Submit<TSection>() where TSection : ISectionModel
        {
            return new FormSectionOperation(OperationType.Submit, typeof(TSection));
        }

        public OperationType Operation { get; }
        public Type SectionType { get; }
    }
}
