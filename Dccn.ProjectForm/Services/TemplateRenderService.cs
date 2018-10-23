using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using HandlebarsDotNet;

namespace Dccn.ProjectForm.Services
{
    public class TemplateRenderService : ITemplateRenderService
    {
        private readonly IHandlebars _handlebars;
        private readonly IDictionary<string, Func<object, string>> _templateCache;

        public TemplateRenderService(IHandlebars handlebars)
        {
            _handlebars = handlebars;
            _templateCache = new ConcurrentDictionary<string, Func<object, string>>();
        }

        public Task<string> RenderAsync<TModel>(string templatePath, TModel model)
        {
            if (!_templateCache.TryGetValue(templatePath, out var template))
            {
                // TODO: Should we queue this on a separate thread?
                template = _handlebars.CompileView(templatePath);
            }

            // TODO: And this?
            return Task.FromResult(template(model));
        }
    }

    public interface ITemplateRenderService
    {
        Task<string> RenderAsync<TModel>(string templatePath, TModel model);
    }
}