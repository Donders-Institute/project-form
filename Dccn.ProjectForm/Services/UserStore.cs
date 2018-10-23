using System;
using System.Threading;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data.Projects;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class UserStore : IUserSecurityStampStore<ProjectsUser>
    {
        private readonly ProjectsDbContext _projectsDbContext;

        public UserStore(ProjectsDbContext projectsDbContext)
        {
            _projectsDbContext = projectsDbContext;
        }

        #region IUserStore
        public Task<IdentityResult> CreateAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> UpdateAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> DeleteAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ProjectsUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return _projectsDbContext.Users.FindAsync(new object[]{userId}, cancellationToken);
        }

        public Task<ProjectsUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return FindByIdAsync(normalizedUserName, cancellationToken);
        }

        public Task<string> GetUserIdAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.DisplayName);
        }

        public Task<string> GetNormalizedUserNameAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            return GetUserIdAsync(user, cancellationToken);
        }

        public Task SetUserNameAsync(ProjectsUser user, string userName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetNormalizedUserNameAsync(ProjectsUser user, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region IUserSecurityStampStore
        public Task SetSecurityStampAsync(ProjectsUser user, string stamp, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetSecurityStampAsync(ProjectsUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
        }
        #endregion
    }
}
