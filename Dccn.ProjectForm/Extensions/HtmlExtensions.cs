using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Dccn.ProjectForm.Extensions
{
    public static class HtmlExtensions
    {
        public static Task<IHtmlContent> FormSectionAsync<TModel>(this IHtmlHelper<TModel> html, string sectionId)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(sectionId, html.ViewData, html.MetadataProvider);
            var viewData = new ViewDataDictionary(html.ViewData)
            {
                TemplateInfo =
                {
                    HtmlFieldPrefix = sectionId
                }
            };

            return html.PartialAsync($"Sections/{sectionId}", modelExplorer.Model, viewData);
        }
    }
}