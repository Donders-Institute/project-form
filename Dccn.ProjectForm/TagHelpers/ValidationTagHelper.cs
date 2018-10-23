using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dccn.ProjectForm.TagHelpers
{
    [HtmlTargetElement("input", Attributes = ForAttributeName)]
    [HtmlTargetElement("textarea", Attributes = ForAttributeName)]
    [HtmlTargetElement("select", Attributes = ForAttributeName)]
    [UsedImplicitly]
    public class ValidationTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";

        [HtmlAttributeNotBound]
        [ViewContext]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public ViewContext ViewContext { private get; set; }

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var fullName = NameAndIdProvider.GetFullHtmlFieldName(ViewContext, For.Name);

            var formContext = ViewContext.ClientValidationEnabled ? ViewContext.FormContext : null;
            if (!ViewContext.ViewData.ModelState.ContainsKey(fullName) && formContext == null)
            {
                return;
            }

            var tryGetModelStateResult = ViewContext.ViewData.ModelState.TryGetValue(fullName, out var entry);
            var modelErrors = tryGetModelStateResult ? entry.Errors : null;

            ModelError modelError = null;
            if (modelErrors != null && modelErrors.Count != 0)
            {
                modelError = modelErrors.FirstOrDefault(m => !string.IsNullOrEmpty(m.ErrorMessage)) ?? modelErrors[0];
            }

            if (modelError != null)
            {
                output.Attributes.Add("data-validation-message", modelError.ErrorMessage);
            }
        }
    }
}
