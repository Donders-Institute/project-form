using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dccn.ProjectForm.TagHelpers
{
    [HtmlTargetElement("small", Attributes = ForAttributeName)]
    [UsedImplicitly]
    public class DescriptionTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-description-for";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "small";
            output.Attributes.SetAttribute("class", "form-text text-muted");
            output.Content.SetContent(For.Metadata.Description);
        }
    }
}
