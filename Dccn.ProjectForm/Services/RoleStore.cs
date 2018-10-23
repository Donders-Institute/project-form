using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class RoleStore<TRole> : IRoleStore<TRole> where TRole: class
    {
        #region IRoleStore<string>
        public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
        }
        #endregion
    }
}
