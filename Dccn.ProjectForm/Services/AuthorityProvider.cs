using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class AuthorityProvider : IAuthorityProvider
    {
        private readonly IDictionary<ApprovalAuthorityRole, string> _authorities;
        private readonly IUserManager _userManager;

        public AuthorityProvider(IOptionsSnapshot<FormOptions> options, IUserManager userManager)
        {
            _authorities = options.Value.Authorities;
            _userManager = userManager;
        }

        public string GetAuthorityId(Proposal proposal, ApprovalAuthorityRole role)
        {
            if (role == ApprovalAuthorityRole.Supervisor)
            {
                return proposal.SupervisorId;
            }

            return _authorities.TryGetValue(role, out var authorityId) ? authorityId : null;
        }

        public IEnumerable<ApprovalAuthorityRole> GetAuthorityRoles(string authorityId)
        {
            return _authorities.Where(entry => entry.Value == authorityId).Select(entry => entry.Key);
        }

        public Task<ProjectsUser> GetAuthorityAsync(Proposal proposal, ApprovalAuthorityRole role)
        {
            var id = GetAuthorityId(proposal, role);
            return _userManager.GetUserByIdAsync(id);
        }
    }

    public interface IAuthorityProvider
    {
        string GetAuthorityId(Proposal proposal, ApprovalAuthorityRole role);
        IEnumerable<ApprovalAuthorityRole> GetAuthorityRoles(string authorityId);
        Task<ProjectsUser> GetAuthorityAsync(Proposal proposal, ApprovalAuthorityRole role);
    }
}
