using System.Threading.Tasks;
using HandlebarsDotNet;
using Microsoft.Extensions.Caching.Memory;

namespace Dccn.ProjectForm.Services
{
    public class TemplateRenderService : ITemplateRenderService
    {
        private readonly IHandlebars _handlebars;
        private readonly IMemoryCache _templateCache;

        public TemplateRenderService(IHandlebars handlebars, IMemoryCache templateCache)
        {
            _handlebars = handlebars;
            _templateCache = templateCache;
        }

        public Task<string> RenderAsync<TModel>(string templatePath, TModel model)
        {
            return Task.FromResult(Render(templatePath, model));
        }

        private string Render<TModel>(string templatePath, TModel model)
        {
            var template = _templateCache.GetOrCreate(templatePath, _ => _handlebars.CompileView(templatePath));
            return template(model).Trim();
        }
    }

    public interface ITemplateRenderService
    {
        Task<string> RenderAsync<TModel>(string templatePath, TModel model);
    }
}