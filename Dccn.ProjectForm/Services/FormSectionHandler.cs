using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
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
        private readonly ModelMetadata _metadata;
        private readonly Func<FormModel, TModel> _compiledExpr;

        protected FormSectionHandlerBase(IServiceProvider serviceProvider,
            Expression<Func<FormModel, TModel>> expression)
        {
            _authorityProvider = serviceProvider.GetService<IAuthorityProvider>();
            _compiledExpr = expression.Compile();
            var id = ExpressionHelper.GetExpressionText(expression); // Note: this is an internal function

            var metadataProvider = serviceProvider.GetService<IModelMetadataProvider>();
            _metadata = metadataProvider.GetMetadataForProperty(typeof(FormModel), id);
        }

        public string Id => _metadata.Name;
        public string DisplayName => _metadata.DisplayName;
        public Type ModelType => _metadata.ModelType;
        // public Type ModelType => typeof(TModel);
        protected abstract IEnumerable<ApprovalAuthorityRole> ApprovalRoles { get; }

        public ISectionModel GetModel(FormModel form)
        {
            return _compiledExpr(form);
        }

        public ICollection<Approval> GetAssociatedApprovals(Proposal proposal)
        {
            return proposal.Approvals
                .Where(a => HasApprovalAuthorityRole(a.AuthorityRole))
                .ToList();
        }

        public bool HasApprovalAuthorityRole(ApprovalAuthorityRole role)
        {
            return ApprovalRoles.Contains(role);
        }

        public virtual IEnumerable<Task<ValidationResult>> ValidateProposalAsync(Proposal proposal)
        {
            return Enumerable.Empty<Task<ValidationResult>>();
        }

        public virtual bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            return true;
        }

        protected virtual async Task LoadAsync(TModel model, Proposal proposal)
        {
            model.Comments = proposal.Comments.FirstOrDefault(c => c.SectionId == ModelType.Name)?.Content;

            model.Approvals = await proposal.Approvals
                .Where(a => ApprovalRoles.Contains(a.AuthorityRole))
                .Select(async a =>
                {
                    var authority = await _authorityProvider.GetAuthorityAsync(proposal, a.AuthorityRole);
                    return new SectionApprovalModel
                    {
                        AuthorityName = authority.DisplayName,
                        AuthorityEmail = authority.Email,
                        Status = (ApprovalStatusModel) a.Status
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

        public Task LoadAsync(FormModel form, Proposal proposal)
        {
            var section = _compiledExpr(form);
            return section == null ? Task.CompletedTask : LoadAsync(section, proposal);
        }

        public Task StoreAsync(FormModel form, Proposal proposal)
        {
            var section = _compiledExpr(form);
            return section == null ? Task.CompletedTask : StoreAsync(section, proposal);
        }
    }

    public interface IFormSectionHandler
    {
        string Id { get; }
        Type ModelType { get; }
        string DisplayName { get; }

        ISectionModel GetModel(FormModel form);
        ICollection<Approval> GetAssociatedApprovals(Proposal proposal);
        bool HasApprovalAuthorityRole(ApprovalAuthorityRole role);

        IEnumerable<Task<ValidationResult>> ValidateProposalAsync(Proposal proposal);
        Task LoadAsync(FormModel form, Proposal proposal);
        Task StoreAsync(FormModel form, Proposal proposal);

        bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole);
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