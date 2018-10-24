using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services.SectionHandlers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services
{
    public abstract class FormSectionHandlerBase<TModel> : IFormSectionHandler<TModel> where TModel : ISectionModel
    {
        private readonly IAuthorityProvider _authorityProvider;

        protected FormSectionHandlerBase(IAuthorityProvider authorityProvider)
        {
            _authorityProvider = authorityProvider;
        }

        public Type ModelType => typeof(TModel);
        protected abstract IEnumerable<ApprovalAuthorityRole> ApprovalRoles { get; }

        public virtual async Task LoadAsync(TModel model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
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
                        AuthorityEmail = authority.Email
                    };
                })
                .ToListAsync();
        }

        public virtual Task StoreAsync(TModel model, Proposal proposal)
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

        public Task LoadAsync(object model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            if (!(model is TModel typedModel))
            {
                throw new ArgumentException(nameof(model));
            }

            return LoadAsync(typedModel, proposal, owner, supervisor);
        }

        public Task StoreAsync(object model, Proposal proposal)
        {
            if (!(model is TModel typedModel))
            {
                throw new ArgumentException(nameof(model));
            }

            return StoreAsync(typedModel, proposal);
        }

        public Task<bool> ValidateExAsync(ModelStateDictionary modelState)
        {
            return Task.FromResult(true);
        }
    }

    public interface IFormSectionHandler<in TModel> : IFormSectionHandler where TModel : ISectionModel
    {
        Task LoadAsync(TModel model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor);
        Task StoreAsync(TModel model, Proposal proposal);
    }

    public interface IFormSectionHandler
    {
        Type ModelType { get; }

        Task<bool> ValidateExAsync(ModelStateDictionary modelState);
        Task LoadAsync(object model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor);
        Task StoreAsync(object model, Proposal proposal);
    }

    public static class FormSectionHandlerExtensions
    {
        public static IServiceCollection AddFormSectionHandlers(this IServiceCollection services)
        {
            services
                .AddTransient<IFormSectionHandler<General>, GeneralHandler>()
                .AddTransient<IFormSectionHandler<Funding>, FundingHandler>()
                .AddTransient<IFormSectionHandler<Ethics>, EthicsHandler>()
                .AddTransient<IFormSectionHandler<Experiment>, ExperimentHandler>()
                .AddTransient<IFormSectionHandler<DataManagement>, DataManagementHandler>()
                .AddTransient<IFormSectionHandler<Privacy>, PrivacyHandler>()
                .AddTransient<IFormSectionHandler<Payment>, PaymentHandler>();

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