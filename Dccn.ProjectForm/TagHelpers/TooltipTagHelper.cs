using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace Dccn.ProjectForm.TagHelpers
{
    [HtmlTargetElement("i", Attributes = TooltipForAttributeName)]
    [HtmlTargetElement("i", Attributes = TooltipAttributeName)]
    [UsedImplicitly]
    public class TooltipTagHelper : TagHelper
    {
        private const string TooltipForAttributeName = "asp-tooltip-for";
        private const string TooltipAttributeName = "asp-tooltip";

        [HtmlAttributeName(TooltipForAttributeName)]
        public ModelExpression TooltipFor { get; set; }

        [HtmlAttributeName(TooltipAttributeName)]
        public string Tooltip { get; set; }

        private readonly IStringLocalizer _localizer;

        public TooltipTagHelper(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var text = Tooltip == null ? TooltipFor.Metadata.Description : _localizer[Tooltip];

            if (text == null)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "span";
            output.Attributes.SetAttribute("data-title", text);
            output.Attributes.SetAttribute("data-toggle", "tooltip");
            output.Attributes.SetAttribute("data-html", "true");

            var tooltip = new TagBuilder("i");
            tooltip.AddCssClass("text-info");
            tooltip.AddCssClass("fas");
            tooltip.AddCssClass("fa-question-circle");

            output.Content.SetHtmlContent(tooltip);
        }
    }
}
