using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dccn.ProjectForm.Pages
{
    [AllowAnonymous]
    public class ErrorModel : PageModel
    {
        public string RequestId { get; private set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        [UsedImplicitly]
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}
