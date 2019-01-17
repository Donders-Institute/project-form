using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dccn.ProjectForm.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

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
                    HtmlFieldPrefix = html.ViewData.TemplateInfo.GetFullHtmlFieldName(section.Id)
                }
            };

            return html.PartialAsync($"Sections/{section.Id}", section, viewData);
        }

        public static IHtmlContent DescriptionFor<TModel, TResult>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression)
        {
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData, html.MetadataProvider);
            return html.Raw(modelExplorer.Metadata.Description);
        }

        public static IHtmlContent Description<TModel>(this IHtmlHelper<TModel> html, string expression)
        {
            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, html.ViewData, html.MetadataProvider);
            return html.Raw(modelExplorer.Metadata.Description);
        }

        public static string DisplayName<TEnum>(this IHtmlHelper html, TEnum @enum) where TEnum : Enum
        {
            var localizer = GetStringLocalizer<TEnum>(html.ViewContext.HttpContext.RequestServices);
            var displayName = GetDisplayAttribute(@enum)?.GetName();
            return displayName == null ? @enum.GetName() : localizer[displayName];
        }

        public static IHtmlContent Description<TEnum>(this IHtmlHelper html, TEnum @enum) where TEnum : Enum
        {
            var localizer = GetHtmlLocalizer<TEnum>(html.ViewContext.HttpContext.RequestServices);
            var description = GetDisplayAttribute(@enum)?.GetDescription();
            return description == null ? null : localizer[description];
        }

        private static IStringLocalizer GetStringLocalizer<T>(IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>().Value;
            var factory = services.GetRequiredService<IStringLocalizerFactory>();
            return options.DataAnnotationLocalizerProvider(typeof(T), factory);
        }

        private static IHtmlLocalizer GetHtmlLocalizer<T>(IServiceProvider services)
        {
            return new HtmlLocalizer(GetStringLocalizer<T>(services));
        }

        private static DisplayAttribute GetDisplayAttribute<TEnum>(TEnum @enum) where TEnum : Enum
        {
            return typeof(TEnum)
                .GetMember(@enum.GetName())
                .First()
                .GetCustomAttribute<DisplayAttribute>();
        }
    }
}