using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data.Projects;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ProjectsUser> _signInManager;
        private readonly ILogger _logger;

        public LoginModel(SignInManager<ProjectsUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [BindProperty]
        [Display(Name = "Stay logged in?")]
        public bool RememberMe { get; set; }

        [UsedImplicitly]
        public IActionResult OnGet(string returnUrl = null)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Page();
            }

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage("/Index");
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(UserName, Password, RememberMe, false);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User '{UserName}' logged in.");

                if (returnUrl != null)
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToPage("/Index");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");

            return Page();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            var userId = _signInManager.UserManager.GetUserId(User);
            await _signInManager.SignOutAsync();

            _logger.LogInformation($"User '{userId}' logged out.");
            return RedirectToPage();
        }
    }
}
