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

        public IEnumerable<string> GetAuthorityIds(Proposal proposal, ApprovalAuthorityRole role)
        {
            if (role == ApprovalAuthorityRole.Supervisor)
            {
                return proposal.SupervisorId.Yield();
            }

            return _formOptions.Authorities.TryGetValue(role, out var authorityId) ? authorityId : Enumerable.Empty<string>();
        }

        public IEnumerable<ApprovalAuthorityRole> GetAuthorityRoles(string authorityId)
        {
            return _formOptions.Authorities.Where(entry => entry.Value.Contains(authorityId)).Select(entry => entry.Key);
        }

        public async Task<ICollection<ProjectDbUser>> GetAuthoritiesAsync(Proposal proposal, ApprovalAuthorityRole role)
        {
            var ids = GetAuthorityIds(proposal, role);
            return await ids.Select(id => _userManager.GetUserByIdAsync(id)).ToListAsync();
        }

        public IEnumerable<string> GetAdministrationIds(Proposal proposal)
        {
            return _formOptions.Admins;
        }

        public async Task<ICollection<ProjectDbUser>> GetAdministrationAsync(Proposal proposal)
        {
            return await _formOptions.Admins.Select(id => _userManager.GetUserByIdAsync(id)).ToListAsync();
        }
    }

    public interface IAuthorityProvider
    {
        IEnumerable<string> GetAuthorityIds(Proposal proposal, ApprovalAuthorityRole role);
        IEnumerable<ApprovalAuthorityRole> GetAuthorityRoles(string authorityId);
        Task<ICollection<ProjectDbUser>> GetAuthoritiesAsync(Proposal proposal, ApprovalAuthorityRole role);
        IEnumerable<string> GetAdministrationIds(Proposal proposal);
        Task<ICollection<ProjectDbUser>> GetAdministrationAsync(Proposal proposal);
    }
}
