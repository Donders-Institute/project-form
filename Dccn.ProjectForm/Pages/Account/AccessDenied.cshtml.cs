using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dccn.ProjectForm.Pages.Account
{
    [AllowAnonymous]
    public class AccessDeniedModel : PageModel
    {
        [UsedImplicitly]
        public void OnGet()
        {

        }
    }
}
