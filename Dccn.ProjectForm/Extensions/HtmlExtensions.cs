using System.Threading.Tasks;
using Dccn.ProjectForm.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Dccn.ProjectForm.Extensions
{
    public static class HtmlExtensions
    {
        public static Task<IHtmlContent> FormSectionAsync<TModel>(this IHtmlHelper<TModel> html, ISectionModel section)
        {
            var viewData = new ViewDataDictionary(html.ViewData)
            {
                TemplateInfo =
                {
                    HtmlFieldPrefix = section.Id
                }
            };

            return html.PartialAsync($"Sections/{section.Id}", section, viewData);
        }
    }
}