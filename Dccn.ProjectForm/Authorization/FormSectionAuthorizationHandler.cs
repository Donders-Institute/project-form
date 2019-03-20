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
            if (_userManager.IsInRole(context.User, Role.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (proposal.ProjectId != null)
            {
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            var sectionHandler =  _sectionHandlers.First(h => h.ModelType == requirement.SectionType);

            if (sectionHandler.NeedsApprovalBy(proposal).Any())
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var approvals =  sectionHandler.GetAssociatedApprovals(proposal);
            switch (requirement.Operation)
            {
                case FormSectionOperation.OperationType.Edit when (userId == proposal.OwnerId || userId == proposal.SupervisorId) && approvals.All(CanOwnerEdit):
                    context.Succeed(requirement);
                    break;
                case FormSectionOperation.OperationType.Edit:
                {
                    var canEdit = approvals
                        .Where(a => _authorityProvider.GetAuthorityIds(proposal, a.AuthorityRole).Contains(userId))
                        .Any(CanAuthorityEdit);
                    if (canEdit)
                    {
                        context.Succeed(requirement);
                    }

                    break;
                }
                case FormSectionOperation.OperationType.Submit when userId == proposal.OwnerId && approvals.All(CanOwnerSubmit):
                    context.Succeed(requirement);
                    break;
                case FormSectionOperation.OperationType.Retract when userId == proposal.OwnerId && approvals.All(CanOwnerRetract):
                    context.Succeed(requirement);
                    break;
            }

            return Task.CompletedTask;
        }

        private static bool CanOwnerEdit(Approval approval)
        {
            return approval.Status == ApprovalStatus.NotSubmitted
                   || approval.Status == ApprovalStatus.NotApplicable;
        }

        private static bool CanOwnerSubmit(Approval approval)
        {
            return approval.Status == ApprovalStatus.NotSubmitted
                   || approval.Status == ApprovalStatus.NotApplicable;
        }

        private static bool CanOwnerRetract(Approval approval)
        {
            return approval.Status == ApprovalStatus.NotApplicable
                   || approval.Status == ApprovalStatus.ApprovalPending
                   || approval.Status == ApprovalStatus.Rejected
                   || approval.Status == ApprovalStatus.Approved;
        }

        private static bool CanAuthorityEdit(Approval approval)
        {
            return approval.Status == ApprovalStatus.ApprovalPending
                   || approval.Status == ApprovalStatus.Rejected;
        }
    }

    public sealed class FormSectionOperation : OperationAuthorizationRequirement
    {
        public enum OperationType
        {
            Edit,
            Submit,
            Retract
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

        public static FormSectionOperation Submit(Type sectionType)
        {
            return new FormSectionOperation(OperationType.Submit, sectionType);
        }

        public static FormSectionOperation Retract(Type sectionType)
        {
            return new FormSectionOperation(OperationType.Retract, sectionType);
        }

        public static FormSectionOperation Edit<TSection>() where TSection : ISectionModel
        {
            return new FormSectionOperation(OperationType.Edit, typeof(TSection));
        }

        public static FormSectionOperation Submit<TSection>() where TSection : ISectionModel
        {
            return new FormSectionOperation(OperationType.Submit, typeof(TSection));
        }

        public static FormSectionOperation Retract<TSection>() where TSection : ISectionModel
        {
            return new FormSectionOperation(OperationType.Retract, typeof(TSection));
        }

        public OperationType Operation { get; }
        public Type SectionType { get; }
    }
}
