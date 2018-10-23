using System;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class AuthorityProvider : IAuthorityProvider
    {
        private readonly FormOptions.AuthorityOptions _options;
        private readonly ProjectsDbContext _dbContext;

        public AuthorityProvider(IOptionsSnapshot<FormOptions> options, ProjectsDbContext dbContext)
        {
            _options = options.Value.Authorities;
            _dbContext = dbContext;
        }

        public string GetAuthorityId(Proposal proposal, ApprovalAuthorityRole role)
        {
            switch (role)
            {
                case ApprovalAuthorityRole.Ethics:
                    return _options.EthicalApproval;
                case ApprovalAuthorityRole.LabMri:
                    return _options.LabCoordinatorMri;
                case ApprovalAuthorityRole.LabOther:
                    return _options.LabCoordinatorOther;
                case ApprovalAuthorityRole.Privacy:
                    return _options.PrivacyOfficer;
                case ApprovalAuthorityRole.Funding:
                    return _options.Funding;
                case ApprovalAuthorityRole.Director:
                    return _options.Director;
                case ApprovalAuthorityRole.Administration:
                    return _options.Administration;
                case ApprovalAuthorityRole.Supervisor:
                    return proposal.SupervisorId;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public Task<ProjectsUser> GetAuthorityAsync(Proposal proposal, ApprovalAuthorityRole role)
        {
            var id = GetAuthorityId(proposal, role);
            return _dbContext.Users.FindAsync(id);
        }
    }

    public interface IAuthorityProvider
    {
        string GetAuthorityId(Proposal proposal, ApprovalAuthorityRole role);
        Task<ProjectsUser> GetAuthorityAsync(Proposal proposal, ApprovalAuthorityRole role);
    }
}
