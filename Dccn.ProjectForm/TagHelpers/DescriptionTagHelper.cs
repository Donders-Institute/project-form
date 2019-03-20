using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dccn.ProjectForm.TagHelpers
{
    [HtmlTargetElement("small", Attributes = DescriptionForAttributeName)]
    [UsedImplicitly]
    public class DescriptionTagHelper : TagHelper
    {
        private const string DescriptionForAttributeName = "asp-description-for";

        [HtmlAttributeName(DescriptionForAttributeName)]
        public ModelExpression DescriptionFor { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var description = DescriptionFor.Metadata.Description;

            if (description == null)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "small";
            output.Attributes.SetAttribute("class", "form-text text-muted");

            var periodIndex = description.IndexOf('.');
            output.Content.SetHtmlContent(description.Substring(0, periodIndex + 1));
            if (description.Length > periodIndex + 1)
            {
                var wrapper = new TagBuilder("span");
                wrapper.AddCssClass("ml-1");
                wrapper.Attributes.Add("data-title", description);
                wrapper.Attributes.Add("data-toggle", "tooltip");

                var tooltip = new TagBuilder("i");
                tooltip.AddCssClass("text-info");
                tooltip.AddCssClass("fas");
                tooltip.AddCssClass("fa-question-circle");

                wrapper.InnerHtml.SetHtmlContent(tooltip);
                output.PostContent.AppendHtml(wrapper);
            }
        }
    }
}
