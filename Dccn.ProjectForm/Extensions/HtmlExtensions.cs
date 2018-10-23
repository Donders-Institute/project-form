using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Dccn.ProjectForm.Extensions
{
    public static class HtmlExtensions
    {
        public static Task<IHtmlContent> FormSectionAsync<TModel>(this IHtmlHelper<TModel> helper, string sectionId)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(sectionId, helper.ViewData, helper.MetadataProvider);
            var viewData = new ViewDataDictionary(helper.ViewData)
            {
                TemplateInfo =
                {
                    HtmlFieldPrefix = sectionId
                }
            };

            return helper.PartialAsync($"Sections/{sectionId}", modelExplorer.Model, viewData);
        }
    }
}