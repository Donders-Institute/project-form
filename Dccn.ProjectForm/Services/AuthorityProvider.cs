using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.Extensions;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class AuthorityProvider : IAuthorityProvider
    {
        private readonly FormOptions _formOptions;
        private readonly IUserManager _userManager;

        public AuthorityProvider(IOptionsSnapshot<FormOptions> options, IUserManager userManager)
        {
            _formOptions = options.Value;
            _userManager = userManager;
        }

        public IEnumerable<string> GetAuthorityIdsForRole(Proposal proposal, ApprovalAuthorityRole role)
        {
            if (role == ApprovalAuthorityRole.Supervisor)
            {
                return proposal.SupervisorId.Yield();
            }

            return _formOptions.Authorities.TryGetValue(role, out var authorityIds)
                ? authorityIds
                : Enumerable.Empty<string>();
        }

        public string GetPrimaryAuthorityId(Proposal proposal, ApprovalAuthorityRole role)
        {
            return GetAuthorityIdsForRole(proposal, role).FirstOrDefault();
        }

        public bool IsPrimaryAuthorityForRole(string userId, ApprovalAuthorityRole role)
        {
            if (role == ApprovalAuthorityRole.Supervisor)
            {
                return true;
            }

            return _formOptions.Authorities.TryGetValue(role, out var authorityIds) && authorityIds.FirstOrDefault() == userId;
        }

        public IEnumerable<ApprovalAuthorityRole> GetAuthorityRoles(string authorityId)
        {
            return _formOptions.Authorities
                .Where(entry => entry.Value.Contains(authorityId))
                .Select(entry => entry.Key);
        }

        public Task<ICollection<ProjectDbUser>> GetAuthoritiesByRoleAsync(Proposal proposal, ApprovalAuthorityRole role)
        {
            return _userManager.GetUsersByIdsAsync(GetAuthorityIdsForRole(proposal, role));
        }

        public async Task<ProjectDbUser> GetPrimaryAuthorityForRoleAsync(Proposal proposal, ApprovalAuthorityRole role)
        {
            var authorityId = GetPrimaryAuthorityId(proposal, role);
            return authorityId == null ? null : await _userManager.GetUserByIdAsync(authorityId);
        }

        public Task<ICollection<ProjectDbUser>> GetAdministrationAsync()
        {
            return _userManager.GetUsersByIdsAsync(_formOptions.Administration);
        }
    }

    public interface IAuthorityProvider
    {
        IEnumerable<string> GetAuthorityIdsForRole(Proposal proposal, ApprovalAuthorityRole role);
        bool IsPrimaryAuthorityForRole(string userId, ApprovalAuthorityRole role);
        IEnumerable<ApprovalAuthorityRole> GetAuthorityRoles(string authorityId);
        Task<ICollection<ProjectDbUser>> GetAuthoritiesByRoleAsync(Proposal proposal, ApprovalAuthorityRole role);
        Task<ICollection<ProjectDbUser>> GetAdministrationAsync();
        string GetPrimaryAuthorityId(Proposal proposal, ApprovalAuthorityRole role);
        Task<ProjectDbUser> GetPrimaryAuthorityForRoleAsync(Proposal proposal, ApprovalAuthorityRole role);
    }
}
