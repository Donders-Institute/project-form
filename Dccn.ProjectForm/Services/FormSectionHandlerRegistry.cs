using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services
{
    public class FormSectionHandlerRegistry : IFormSectionHandlerRegistry
    {
        private readonly IDictionary<Type, IFormSectionHandler> _handlers;

        public FormSectionHandlerRegistry(IEnumerable<IFormSectionHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.ModelType);
        }

        public IFormSectionHandler GetHandler(Type type)
        {
            if (!type.IsSubclassOf(typeof(ISectionModel)))
            {
                throw new ArgumentException(nameof(type));
            }

            return _handlers[type];
        }

        public IFormSectionHandler<TModel> GetHandler<TModel>() where TModel : ISectionModel
        {
            return (IFormSectionHandler<TModel>) GetHandler(typeof(TModel));
        }

        public Task LoadSectionAsync(object model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            return _handlers[model.GetType()].LoadAsync(model, proposal, owner, supervisor);
        }

        public Task StoreSectionAsync(object model, Proposal proposal)
        {
            return _handlers[model.GetType()].StoreAsync(model, proposal);
        }
    }

    public interface IFormSectionHandlerRegistry
    {
        IFormSectionHandler GetHandler(Type type);
        IFormSectionHandler<TModel> GetHandler<TModel>() where TModel : ISectionModel;
        Task LoadSectionAsync(object model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor);
        Task StoreSectionAsync(object model, Proposal proposal);
    }
}