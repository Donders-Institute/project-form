using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.Extensions;
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
        private readonly IAuthorityProvider _authorityProvider;
        private readonly IUserManager _userManager;
        private readonly IValidator<TModel> _validator;
        private readonly ModelMetadata _metadata;
        private readonly Func<FormModel, TModel> _compiledExpr;

        protected FormSectionHandlerBase(IServiceProvider serviceProvider, Expression<Func<FormModel, TModel>> expression)
        {
            _authorityProvider = serviceProvider.GetRequiredService<IAuthorityProvider>();
            _userManager = serviceProvider.GetRequiredService<IUserManager>();
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

        protected virtual async Task LoadAsync(TModel model, Proposal proposal)
        {
            model.Id = Id;

            model.Comments = proposal.Comments.FirstOrDefault(c => c.SectionId == Id)?.Content;

            model.Approvals = await proposal.Approvals
                .Where(a => ApprovalRoles.Contains(a.AuthorityRole))
                .Select(async approval =>
                {
                    var authorities = await _authorityProvider.GetAuthoritiesAsync(proposal, approval.AuthorityRole);
                    ProjectDbUser authority;
                    if (approval.Status == ApprovalStatus.Approved || approval.Status == ApprovalStatus.Rejected)
                    {
                        // Show authority who approved the section at the time as opposed to current approval role
                        authority = await _userManager.GetUserByIdAsync(approval.ValidatedBy);
                    }
                    else
                    {
                        // Pick first from the list (if any)
                        authority = authorities.FirstOrDefault();
                    }

                    return new SectionApprovalModel
                    {
                        RawApproval = approval,
                        AuthorityRole = (ApprovalAuthorityRoleModel) approval.AuthorityRole,
                        AuthorityName = authority?.DisplayName,
                        AuthorityEmail = authority?.Email,
                        Status = (ApprovalStatusModel) approval.Status
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

        bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole);
        IEnumerable<ApprovalAuthorityRole> NeedsApprovalBy(Proposal proposal);
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