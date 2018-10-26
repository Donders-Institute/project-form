using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data.Projects;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;

namespace Dccn.ProjectForm.Authentication
{
    [UsedImplicitly]
    public class SignInManager : ISignInManager
    {
        private readonly IHostingEnvironment _environment;
        private readonly LdapOptions _ldapOptions;
        private readonly ILogger _logger;

        public SignInManager(IHostingEnvironment environment, IOptionsSnapshot<LdapOptions> ldapOptions, IUserManager userManager, ILogger<SignInManager> logger)
        {
            _environment = environment;
            _ldapOptions = ldapOptions.Value;
            _logger = logger;
            UserManager = userManager;
        }

        public IUserManager UserManager { get; }

        public async Task<bool> PasswordSignInAsync(HttpContext httpContext, string userId, string password, bool isPersistent)
        {
            var user = await UserManager.GetUserByIdAsync(userId, true);
            if (user == null)
            {
                _logger.LogInformation($"User '{userId}' does not exist.");
                return false;
            }

            if (!CheckPasswordSignIn(user, password))
            {
                _logger.LogInformation($"Invalid credentials supplied for '{userId}'.");
                return false;
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.UserName, ClaimTypes.Role);

            identity.AddClaim(new Claim(ClaimTypes.UserId, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.UserName, user.DisplayName));
            identity.AddClaim(new Claim(ClaimTypes.EmailAddress, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Group, user.GroupId));
            if (user.IsHead)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, Enum.GetName(typeof(Role), Role.Supervisor)));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            return true;
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public bool IsSignedIn(ClaimsPrincipal user)
        {
            return user.Identity.IsAuthenticated && user.Identity.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme;
        }

        private bool CheckPasswordSignIn(ProjectsUser user, string password)
        {
            if (_environment.IsDevelopment())
            {
                return true;
            }

            var userId = user.Id;
            using (var connection = new LdapConnection { SecureSocketLayer = _ldapOptions.UseSsl })
            {
                var port = _ldapOptions.Port ?? (_ldapOptions.UseSsl ? LdapConnection.DEFAULT_SSL_PORT : LdapConnection.DEFAULT_PORT);
                try
                {
                    if (_ldapOptions.Hosts != null)
                    {
                        connection.Connect(_ldapOptions.Hosts.Join(" "), port);
                    }
                    else if (_ldapOptions.Host != null)
                    {
                        connection.Connect(_ldapOptions.Host, port);
                    }
                    else if (_ldapOptions.Domain != null)
                    {
                        connection.Connect(_ldapOptions.Domain, port);
                    }
                    else
                    {
                        connection.Connect("localhost", port);
                    }

                    connection.Bind(_ldapOptions.Domain != null ? $"{userId}@{_ldapOptions.Domain}" : userId, password);

                    return connection.Bound;
                }
                catch (LdapException e) when (e.ResultCode == LdapException.INVALID_CREDENTIALS)
                {
                    return false;
                }
            }
        }
    }

    public interface ISignInManager
    {
        Task<bool> PasswordSignInAsync(HttpContext httpContext, string userId, string password, bool isPersistent);
        Task SignOutAsync(HttpContext httpContext);
        bool IsSignedIn(ClaimsPrincipal user);
        IUserManager UserManager { get; }
    }
}
