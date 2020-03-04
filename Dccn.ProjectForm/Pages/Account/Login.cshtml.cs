using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ISignInManager _signInManager;
        private readonly ILogger _logger;

        public LoginModel(ISignInManager signInManager, ILogger<LoginModel> logger)
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

            switch (await _signInManager.PasswordSignInAsync(HttpContext, UserName, Password, RememberMe))
            {
                case SignInStatus.Success:
                    _logger.LogInformation($"User '{UserName}' logged in.");

                    if (returnUrl != null)
                    {
                        return LocalRedirect(returnUrl);
                    }

                    return RedirectToPage("/Index");
                case SignInStatus.InvalidCredentials:
                    ModelState.AddModelError(nameof(Password), "Invalid login attempt.");
                    break;
                case SignInStatus.InvalidStatus:
                    ModelState.AddModelError(nameof(UserName), "Your account has not yet been fully activated. Please make sure you finish the check-in procedure. For more information you can contact DCCN/CNS administration.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Page();
        }

        [UsedImplicitly]
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            var userId = _signInManager.UserManager.GetUserId(User);
            await _signInManager.SignOutAsync(HttpContext);

            _logger.LogInformation($"User '{userId}' logged out.");
            return RedirectToPage();
        }
    }
}
