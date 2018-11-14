using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Pages;
using Dccn.ProjectForm.Services.SectionHandlers;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services
{
    public abstract class FormSectionHandlerBase<TModel> : IFormSectionHandler<TModel> where TModel : ISectionModel, new()
    {
        private readonly IAuthorityProvider _authorityProvider;
        private readonly IValidator<TModel> _validator;
        private readonly ModelMetadata _metadata;
        private readonly Func<FormModel, TModel> _compiledExpr;

        protected FormSectionHandlerBase(IServiceProvider serviceProvider, Expression<Func<FormModel, TModel>> expression)
        {
            _authorityProvider = serviceProvider.GetRequiredService<IAuthorityProvider>();
            _validator = serviceProvider.GetService<IValidator<TModel>>() ?? new InlineValidator<TModel>();
            _compiledExpr = expression.Compile();

            var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
            var id = ExpressionHelper.GetExpressionText(expression);
            _metadata = metadataProvider.GetMetadataForProperty(typeof(FormModel), id);
        }

        public string Id => _metadata.Name;
        public string DisplayName => _metadata.DisplayName;
        public Type ModelType => _metadata.ModelType;

        protected abstract IEnumerable<ApprovalAuthorityRole> ApprovalRoles { get; }

        public ISectionModel GetModel(FormModel form)
        {
            return ((IFormSectionHandler<TModel>) this).GetModel(form);
        }

        TModel IFormSectionHandler<TModel>.GetModel(FormModel form)
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

        public Task<bool> ValidateInputAsync(FormModel form)
        {
            return ValidateAsync(form, false);
        }

        public async Task<bool> ValidateProposalAsync(FormModel form, Proposal proposal)
        {
            await LoadAsync(form, proposal);
            return await ValidateAsync(form, true);
        }

        public virtual bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            return true;
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

        protected virtual async Task LoadAsync(TModel model, Proposal proposal)
        {
            model.Id = Id;

            model.Comments = proposal.Comments.FirstOrDefault(c => c.SectionId == Id)?.Content;

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
            var comment = proposal.Comments.FirstOrDefault(c => c.SectionId == Id);
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
                    SectionId = Id,
                    Content = model.Comments
                });
            }

            return Task.CompletedTask;
        }

        private async Task<bool> ValidateAsync(FormModel form, bool full)
        {
            if (_validator == null)
            {
                return true;
            }

            var result = await _validator.ValidateAsync(_compiledExpr(form), ruleSet: full ? "default,Submit" : null);
            result.AddToModelState(form.ModelState, Id);
            return result.IsValid;
        }
    }

    public interface IFormSectionHandler<out TModel> : IFormSectionHandler
    {
        new TModel GetModel(FormModel form);
    }

    public interface IFormSectionHandler
    {
        string Id { get; }
        Type ModelType { get; }
        string DisplayName { get; }

        ISectionModel GetModel(FormModel form);
        ICollection<Approval> GetAssociatedApprovals(Proposal proposal);
        bool HasApprovalAuthorityRole(ApprovalAuthorityRole role);

        Task<bool> ValidateInputAsync(FormModel form);
        Task<bool> ValidateProposalAsync(FormModel form, Proposal proposal);
        Task LoadAsync(FormModel form, Proposal proposal);
        Task StoreAsync(FormModel form, Proposal proposal);

        bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole);
    }

    public static class FormSectionHandlerExtensions
    {
        public static IServiceCollection AddFormSectionHandlers(this IServiceCollection services)
        {
            services
                .AddTransient<IFormSectionHandler<GeneralSectionModel>, GeneralSectionHandler>()
                .AddTransient<IFormSectionHandler<FundingSectionModel>, FundingSectionHandler>()
                .AddTransient<IFormSectionHandler<EthicsSectionModel>, EthicsSectionHandler>()
                .AddTransient<IFormSectionHandler<ExperimentSectionModel>, ExperimentSectionHandler>()
                .AddTransient<IFormSectionHandler<DataSectionModel>, DataSectionHandler>()
                .AddTransient<IFormSectionHandler<PrivacySectionModel>, PrivacySectionHandler>()
                .AddTransient<IFormSectionHandler<PaymentSectionModel>, PaymentSectionHandler>();

            services
                .AddTransient<IFormSectionHandler, GeneralSectionHandler>()
                .AddTransient<IFormSectionHandler, FundingSectionHandler>()
                .AddTransient<IFormSectionHandler, EthicsSectionHandler>()
                .AddTransient<IFormSectionHandler, ExperimentSectionHandler>()
                .AddTransient<IFormSectionHandler, DataSectionHandler>()
                .AddTransient<IFormSectionHandler, PrivacySectionHandler>()
                .AddTransient<IFormSectionHandler, PaymentSectionHandler>();

            return services;
        }
    }
}