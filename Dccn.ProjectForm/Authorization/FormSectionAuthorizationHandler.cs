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
        private readonly IEnumerable<IFormSectionHandler> _sectionHandlers;

        public FormSectionAuthorizationHandler(IUserManager userManager, IEnumerable<IFormSectionHandler> sectionHandlers)
        {
            _userManager = userManager;
            _sectionHandlers = sectionHandlers;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FormSectionOperation requirement, Proposal proposal)
        {
            // Admin can do anything
            if (_userManager.IsInRole(context.User, Role.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Project ID has been assigned so the proposal is in a read-only state
            if (proposal.ProjectId != null)
            {
                return Task.CompletedTask;
            }

            var sectionHandler =  _sectionHandlers.Single(h => h.ModelType == requirement.SectionType);
            // Section's prerequisites have not been met
            if (sectionHandler.NeedsApprovalBy(proposal).Any())
            {
                return Task.CompletedTask;
            }

            var userId = _userManager.GetUserId(context.User);
            var approvals =  sectionHandler.GetAssociatedApprovals(proposal);
            var success = false;
            switch (requirement.Operation)
            {
                case FormSectionOperation.OperationType.Edit:
                    // User and supervisor can edit if section's approval state allows it
                    if (userId == proposal.OwnerId || userId == proposal.SupervisorId)
                    {
                        success = CanOwnerOrSupervisorEdit(approvals);
                    }

                    // Associated authorities can edit if section's approval state allows it
                    if (approvals.Any(approval => _userManager.IsInApprovalRole(context.User, approval.AuthorityRole)))
                    {
                        success = CanAuthorityEdit(approvals);
                    }

                    break;
                case FormSectionOperation.OperationType.Submit:
                    // User can edit if section's approval state allows it
                    success = userId == proposal.OwnerId && CanOwnerSubmit(approvals);
                    break;
                case FormSectionOperation.OperationType.Retract:
                    // User can retract if section's approval state allows it
                    success = userId == proposal.OwnerId && CanOwnerRetract(approvals);
                    break;
            }

            if (success)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private static bool CanOwnerOrSupervisorEdit(IEnumerable<Approval> approvals)
        {
            return approvals.All(approval => approval.Status == ApprovalStatus.NotSubmitted ||
                                             approval.Status == ApprovalStatus.NotApplicable);
        }

        private static bool CanOwnerSubmit(IEnumerable<Approval> approvals)
        {
            return approvals.All(approval => approval.Status == ApprovalStatus.NotSubmitted ||
                                             approval.Status == ApprovalStatus.NotApplicable);
        }

        private static bool CanOwnerRetract(IEnumerable<Approval> approvals)
        {
            return approvals.All(approval => approval.Status == ApprovalStatus.NotApplicable ||
                                             approval.Status == ApprovalStatus.ApprovalPending ||
                                             approval.Status == ApprovalStatus.Rejected ||
                                             approval.Status == ApprovalStatus.Approved);
        }

        private static bool CanAuthorityEdit(IEnumerable<Approval> approvals)
        {
            return approvals.Any(approval => approval.Status == ApprovalStatus.ApprovalPending ||
                                             approval.Status == ApprovalStatus.Rejected);

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
