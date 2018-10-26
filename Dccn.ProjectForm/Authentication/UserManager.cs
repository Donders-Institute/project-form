using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data.Projects;

namespace Dccn.ProjectForm.Authentication
{
    public class UserManager : IUserManager
    {
        private readonly ProjectsDbContext _dbContext;

        public UserManager(ProjectsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string GetUserId(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.UserId).Value;
        }

        public string GetUserName(ClaimsPrincipal principal)
        {
            return principal.Identity.Name;
        }

        public string GetEmailAddress(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.EmailAddress).Value;
        }

        public string GetPrimaryGroupId(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Group).Value;
        }

        public IEnumerable<Role> GetRoles(ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role).Select(r => Enum.Parse<Role>(r.Value));
        }

        public bool IsInRole(ClaimsPrincipal principal, Role role)
        {
            return principal.IsInRole(Enum.GetName(typeof(Role), role));
        }

        public Task<ProjectsGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal)
        {
            return GetPrimaryGroupByIdAsync(GetPrimaryGroupId(principal));
        }

        public Task<ProjectsGroup> GetPrimaryGroupByIdAsync(string groupId)
        {
            return _dbContext.Groups.FindAsync(groupId);
        }

        public Task<ProjectsUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup)
        {
            return GetUserByIdAsync(GetUserId(principal), includeGroup);
        }

        public async Task<ProjectsUser> GetUserByIdAsync(string userId, bool includeGroup)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            if (includeGroup)
            {
                await _dbContext.Entry(user).Reference(u => u.Group).LoadAsync();
            }

            return user;
        }
    }

    public interface IUserManager
    {
        string GetUserId(ClaimsPrincipal principal);
        string GetUserName(ClaimsPrincipal principal);
        string GetEmailAddress(ClaimsPrincipal principal);
        string GetPrimaryGroupId(ClaimsPrincipal principal);
        IEnumerable<Role> GetRoles(ClaimsPrincipal principal);
        bool IsInRole(ClaimsPrincipal principal, Role role);

        Task<ProjectsGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal);
        Task<ProjectsGroup> GetPrimaryGroupByIdAsync(string groupId);

        Task<ProjectsUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup = false);
        Task<ProjectsUser> GetUserByIdAsync(string userId, bool includeGroup = false);
    }
}