using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class LdapSignInManager<TUser> : SignInManager<TUser> where TUser: class
    {
        private readonly IHostingEnvironment _environment;
        private readonly LdapOptions _ldapOptions;

        public LdapSignInManager(
            IHostingEnvironment environment,
            IOptionsSnapshot<LdapOptions> ldapOptionsAccesor,
            UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<LdapSignInManager<TUser>> logger,
            IAuthenticationSchemeProvider schemes)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes)
        {
            _environment = environment;
            _ldapOptions = ldapOptionsAccesor.Value;
        }

        private bool TryAuthorizeLdap(string userName, string password)
        {
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

                    connection.Bind(_ldapOptions.Domain != null ? $"{userName}@{_ldapOptions.Domain}" : userName, password);

                    return connection.Bound;
                }
                catch (LdapException e) when (e.ResultCode == LdapException.INVALID_CREDENTIALS)
                {
                    Logger.LogWarning(e, $"Invalid credentials supplied for '{userName}'.");
                    return false;
                }
            }
        }

        public override async Task<SignInResult> CheckPasswordSignInAsync(TUser user, string password, bool lockoutOnFailure)
        {
            if (_environment.IsDevelopment())
            {
                return SignInResult.Success;
            }

            var id = await UserManager.GetUserIdAsync(user);
            return TryAuthorizeLdap(id, password) ? SignInResult.Success : SignInResult.Failed;
        }
    }
}
