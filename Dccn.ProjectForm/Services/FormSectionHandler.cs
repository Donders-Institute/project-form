using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Pages;
using Dccn.ProjectForm.Services.SectionHandlers;
using Dccn.ProjectForm.Services.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services
{
    public abstract class FormSectionHandlerBase<TModel> : IFormSectionHandler<TModel>
        where TModel : ISectionModel, new()
    {
        private readonly IValidator<TModel> _validator;
        private readonly ModelMetadata _metadata;
        private readonly Func<FormModel, TModel> _compiledExpr;

        protected FormSectionHandlerBase(IServiceProvider serviceProvider, Expression<Func<FormModel, TModel>> expression)
        {
            _validator = serviceProvider.GetService<IValidator<TModel>>();
            _compiledExpr = expression.Compile();

            var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
            var id = ExpressionHelper.GetExpressionText(expression);
            _metadata = metadataProvider.GetMetadataForProperty(typeof(FormModel), id);
        }

        public string Id => _metadata.Name;
        public string DisplayName => _metadata.DisplayName;
        public Type ModelType => _metadata.ModelType;

        protected abstract IEnumerable<ApprovalAuthorityRole> ApprovalRoles { get; }
        protected virtual IEnumerable<ApprovalAuthorityRole> RequiredApprovalRoles => Enumerable.Empty<ApprovalAuthorityRole>();

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

        public IEnumerable<ApprovalAuthorityRole> NeedsApprovalBy(Proposal proposal)
        {
            return proposal.Approvals
                .Where(a => RequiredApprovalRoles.Contains(a.AuthorityRole))
                .Where(a => a.Status != ApprovalStatus.Approved && a.Status != ApprovalStatus.NotApplicable)
                .Select(a => a.AuthorityRole)
                .ToList();
        }

        public Task<bool> ValidateInputAsync(FormModel form)
        {
            return ValidateAsync(form);
        }

        public async Task<bool> ValidateSubmissionAsync(FormModel form, Proposal proposal)
        {
            await LoadAsync(form, proposal);
            return await ValidateAsync(form, "default,Submit");
        }

        public async Task<bool> ValidateApprovalAsync(FormModel form, Proposal proposal)
        {
            await LoadAsync(form, proposal);
            return await ValidateAsync(form, "default,Submit,Approve");
        }

        public async Task<ISectionModel> LoadAsync(Proposal proposal)
        {
            var model = new TModel();
            await LoadAsync(model, proposal);
            return model;
        }

        public virtual bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            if (!ApprovalRoles.Contains(authorityRole))
            {
                throw new ArgumentOutOfRangeException(nameof(authorityRole));
            }

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

        protected virtual Task LoadAsync(TModel model, Proposal proposal)
        {
            model.Id = Id;
            model.ProposalId = proposal.Id;

            model.Comments = proposal.Comments.FirstOrDefault(c => c.SectionId == Id)?.Content;

            return Task.CompletedTask;
        }

        protected virtual Task StoreAsync(TModel model, Proposal proposal)
        {
            var comment = proposal.Comments.SingleOrDefault(c => c.SectionId == Id);
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

        public virtual bool SectionEquals(Proposal x, Proposal y) =>
            x.ProjectId == y.ProjectId
            && NeedsApprovalBy(x).Any() == NeedsApprovalBy(y).Any()
            && x.Comments.SingleOrDefault(c => c.SectionId == Id)?.Content == y.Comments.SingleOrDefault(c => c.SectionId == Id)?.Content
            && CompareKeyedCollections(GetAssociatedApprovals(x), GetAssociatedApprovals(y), a => a.AuthorityRole, (ax, ay) =>
                ax.AuthorityRole == ay.AuthorityRole
                && ax.Status == ay.Status
                && ax.ValidatedBy == ay.ValidatedBy);


        private async Task<bool> ValidateAsync(FormModel form, string ruleSet = null)
        {
            if (_validator == null)
            {
                return true;
            }

            var result = await _validator.ValidateAsync(_compiledExpr(form), ruleSet: ruleSet);
            result.AddToModelState(form.ModelState, Id);
            return result.IsValid;
        }

        protected static bool CompareKeyedCollections<TElement>(IEnumerable<TElement> x, IEnumerable<TElement> y) where TElement : IComparable<TElement>
        {
            return CompareKeyedCollections(x, y, k => k);
        }

        protected static bool CompareKeyedCollections<TElement, TKey>(IEnumerable<TElement> x, IEnumerable<TElement> y, Func<TElement, TKey> keySelector)
        {
            return CompareKeyedCollections(x, y, keySelector, (ex, ey) => ex.Equals(ey));
        }

        protected static bool CompareKeyedCollections<TElement, TKey>(IEnumerable<TElement> x, IEnumerable<TElement> y, Func<TElement, TKey> keySelector, Func<TElement, TElement, bool> comparer)
        {
            return x.OrderBy(keySelector).SequenceEqual(y.OrderBy(keySelector), new FuncComparer<TElement>(comparer));
        }

        private class FuncComparer<TElement> : IEqualityComparer<TElement>
        {
            private readonly Func<TElement, TElement, bool> _func;

            public FuncComparer(Func<TElement, TElement, bool> func)
            {
                _func = func;
            }

            public bool Equals(TElement x, TElement y)
            {
                return _func(x, y);
            }

            public int GetHashCode(TElement obj)
            {
                throw new NotSupportedException();
            }
        }
    }

    public interface IFormSectionHandler<out TModel> : IFormSectionHandler where TModel : ISectionModel
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
        Task<bool> ValidateSubmissionAsync(FormModel form, Proposal proposal);
        Task<bool> ValidateApprovalAsync(FormModel form, Proposal proposal);
        Task LoadAsync(FormModel form, Proposal proposal);
        Task StoreAsync(FormModel form, Proposal proposal);
        Task<ISectionModel> LoadAsync(Proposal proposal);

        bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole);
        IEnumerable<ApprovalAuthorityRole> NeedsApprovalBy(Proposal proposal);
        bool SectionEquals(Proposal x, Proposal y);
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
                .AddTransient<IFormSectionHandler<PaymentSectionModel>, PaymentSectionHandler>()
                .AddTransient<IFormSectionHandler<SubmissionSectionModel>, SubmissionSectionHandler>();

            services
                .AddTransient<IFormSectionHandler, GeneralSectionHandler>()
                .AddTransient<IFormSectionHandler, FundingSectionHandler>()
                .AddTransient<IFormSectionHandler, EthicsSectionHandler>()
                .AddTransient<IFormSectionHandler, ExperimentSectionHandler>()
                .AddTransient<IFormSectionHandler, DataSectionHandler>()
                .AddTransient<IFormSectionHandler, PrivacySectionHandler>()
                .AddTransient<IFormSectionHandler, PaymentSectionHandler>()
                .AddTransient<IFormSectionHandler, SubmissionSectionHandler>();

            return services;
        }

        public static IServiceCollection AddFormSectionValidators(this IServiceCollection services)
        {
            services
                .AddTransient<IValidator<GeneralSectionModel>, GeneralSectionValidator>()
                .AddTransient<IValidator<FundingSectionModel>, FundingSectionValidator>()
                .AddTransient<IValidator<EthicsSectionModel>, EthicsSectionValidator>()
                .AddTransient<IValidator<ExperimentSectionModel>, ExperimentSectionValidator>()
                .AddTransient<IValidator<DataSectionModel>, DataSectionValidator>()
                .AddTransient<IValidator<PrivacySectionModel>, PrivacySectionValidator>()
                .AddTransient<IValidator<PaymentSectionModel>, PaymentSectionValidator>();

            return services;
        }
    }
}