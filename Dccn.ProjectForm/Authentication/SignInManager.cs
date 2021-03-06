﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Services;
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
        public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        private readonly IHostingEnvironment _environment;
        private readonly IAuthorityProvider _authorityProvider;
        private readonly LdapOptions _ldapOptions;
        private readonly FormOptions _formOptions;
        private readonly ILogger _logger;

        public SignInManager(
            IHostingEnvironment environment,
            IAuthorityProvider authorityProvider,
            IOptionsSnapshot<LdapOptions> ldapOptions,
            IOptionsSnapshot<FormOptions> formOptions,
            ILogger<SignInManager> logger,
            IUserManager userManager
        ) {
            _environment = environment;
            _authorityProvider = authorityProvider;
            _ldapOptions = ldapOptions.Value;
            _formOptions = formOptions.Value;
            _logger = logger;
            UserManager = userManager;
        }

        public IUserManager UserManager { get; }

        public async Task<SignInStatus> PasswordSignInAsync(HttpContext httpContext, string userId, string password, bool isPersistent)
        {
            var user = await UserManager.GetUserByIdAsync(userId, true);
            if (user == null)
            {
                _logger.LogInformation($"User '{userId}' does not exist.");
                return SignInStatus.InvalidCredentials;
            }

            if (!CheckPasswordSignIn(user, password))
            {
                _logger.LogInformation($"Invalid credentials supplied for '{userId}'.");
                return SignInStatus.InvalidCredentials;
            }

            if (user.Status == CheckinStatus.Tentative)
            {
                _logger.LogInformation($"User '{userId} has tentative status.");
                return SignInStatus.InvalidStatus;
            }

            var identity = new ClaimsIdentity(AuthenticationScheme, ClaimTypes.UserName, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.UserId, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.UserName, user.DisplayName));
            identity.AddClaim(new Claim(ClaimTypes.EmailAddress, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Group, user.GroupId));

            var authorityRoles = _authorityProvider.GetAuthorityRoles(user.Id).ToList();
            if (authorityRoles.Any() || user.IsHead)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, Role.Authority.GetName()));
                identity.AddClaims(authorityRoles.Select(role => new Claim(ClaimTypes.ApprovalRole, role.GetName())));
            }

            if (user.IsHead)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, Role.Supervisor.GetName()));
            }

            if (_formOptions.Administration.Contains(user.Id))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, Role.Administration.GetName()));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };

            await httpContext.SignInAsync(AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            return SignInStatus.Success;
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(AuthenticationScheme);
        }

        public bool IsSignedIn(ClaimsPrincipal user)
        {
            return user.Identity.IsAuthenticated && user.Identity.AuthenticationType == AuthenticationScheme;
        }

        private bool CheckPasswordSignIn(ProjectDbUser user, string password)
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
        Task<SignInStatus> PasswordSignInAsync(HttpContext httpContext, string userId, string password, bool isPersistent);
        Task SignOutAsync(HttpContext httpContext);
        bool IsSignedIn(ClaimsPrincipal user);
        IUserManager UserManager { get; }
    }

    public enum SignInStatus
    {
        Success,
        InvalidCredentials,
        InvalidStatus
    }
}
