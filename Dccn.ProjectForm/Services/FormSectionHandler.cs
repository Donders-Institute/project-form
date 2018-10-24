using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Pages;
using Dccn.ProjectForm.Services.SectionHandlers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services
{
    public abstract class FormSectionHandlerBase<TModel> : IFormSectionHandler where TModel : ISectionModel
    {
        private readonly IAuthorityProvider _authorityProvider;
        private readonly Func<FormModel, TModel> _compiledExpr;

        protected FormSectionHandlerBase(IAuthorityProvider authorityProvider, Expression<Func<FormModel, TModel>> expression)
        {
            _authorityProvider = authorityProvider;
            _compiledExpr = expression.Compile();
            ModelExpression = expression.UpcastFuncResult<FormModel, TModel, ISectionModel>();
            ModelId = ExpressionHelper.GetExpressionText(ModelExpression); // Note: this is an internal function
        }

        public Type ModelType => typeof(TModel);
        protected abstract IEnumerable<ApprovalAuthorityRole> ApprovalRoles { get; }
        public Expression<Func<FormModel, ISectionModel>> ModelExpression { get; }
        public string ModelId { get; }

        public ISectionModel GetModel(FormModel form)
        {
            return _compiledExpr(form);
        }

        protected virtual async Task LoadAsync(TModel model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.Comments = proposal.Comments.FirstOrDefault(c => c.SectionId == ModelType.Name)?.Content;

            model.ApprovalInfo = await proposal.Approvals
                .Where(a => ApprovalRoles.Contains(a.AuthorityRole))
                .Select(async a =>
                {
                    var authority = await _authorityProvider.GetAuthorityAsync(proposal, a.AuthorityRole);
                    return new SectionApprovalInfo
                    {
                        AuthorityName = authority.DisplayName,
                        AuthorityEmail = authority.Email,
                        Status = a.Status
                    };
                })
                .ToListAsync();
        }

        protected virtual Task StoreAsync(TModel model, Proposal proposal)
        {
            var comment = proposal.Comments.FirstOrDefault(c => c.SectionId == ModelType.Name);
            if (comment != null)
            {
                if (string.IsNullOrWhiteSpace(model.Comments))
                {
                    proposal.Comments.Remove(comment);
                }
                else
                {
                    comment.Content = model.Comments;
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Comments))
            {
                proposal.Comments.Add(new Comment
                {
                    SectionId = ModelType.Name,
                    Content = model.Comments
                });
            }

            return Task.CompletedTask;
        }

        public Task LoadAsync(FormModel form, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            var section = _compiledExpr(form);
            return section == null ? Task.CompletedTask : LoadAsync(section, proposal, owner, supervisor);
        }

        public Task StoreAsync(FormModel form, Proposal proposal)
        {
            var section = _compiledExpr(form);
            return section == null ? Task.CompletedTask : StoreAsync(section, proposal);
        }

        protected virtual bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            return true;
        }

        public bool RequestApproval(Proposal proposal)
        {
            var statusChanged = false;

            foreach (var approval in proposal.Approvals.Where(a => ApprovalRoles.Contains(a.AuthorityRole)))
            {
                var oldStatus = approval.Status;
                if (approval.Status == ApprovalStatus.NotSubmitted
                    || approval.Status == ApprovalStatus.NotApplicable
                    || approval.Status == ApprovalStatus.Rejected)
                {
                    approval.Status = IsAuthorityApplicable(proposal, approval.AuthorityRole)
                        ? ApprovalStatus.ApprovalPending
                        : ApprovalStatus.NotApplicable;
                }

                statusChanged = statusChanged || oldStatus != approval.Status;
            }

            return statusChanged;
        }
    }

    public interface IFormSectionHandler
    {
        Type ModelType { get; }
        Expression<Func<FormModel, ISectionModel>> ModelExpression { get; }
        string ModelId { get; }

        ISectionModel GetModel(FormModel form);

        Task LoadAsync(FormModel form, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor);
        Task StoreAsync(FormModel form, Proposal proposal);

        bool RequestApproval(Proposal proposal);
    }

    public static class FormSectionHandlerExtensions
    {
        public static IServiceCollection AddFormSectionHandlers(this IServiceCollection services)
        {
            services
                .AddTransient<IFormSectionHandler, GeneralHandler>()
                .AddTransient<IFormSectionHandler, FundingHandler>()
                .AddTransient<IFormSectionHandler, EthicsHandler>()
                .AddTransient<IFormSectionHandler, ExperimentHandler>()
                .AddTransient<IFormSectionHandler, DataManagementHandler>()
                .AddTransient<IFormSectionHandler, PrivacyHandler>()
                .AddTransient<IFormSectionHandler, PaymentHandler>();

            return services;
        }
    }
}