using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Microsoft.EntityFrameworkCore;

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
            return principal.FindFirst(ClaimTypes.UserId)?.Value;
        }

        public string GetUserName(ClaimsPrincipal principal)
        {
            return principal.Identity.Name;
        }

        public string GetEmailAddress(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.EmailAddress)?.Value;
        }

        public string GetPrimaryGroupId(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Group)?.Value;
        }

        public IEnumerable<ApprovalAuthorityRole> GetRoles(ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role).Select(r => Enum.Parse<ApprovalAuthorityRole>(r.Value));
        }

        public bool IsInRole(ClaimsPrincipal principal, ApprovalAuthorityRole role)
        {
            return principal.IsInRole(Enum.GetName(typeof(ApprovalAuthorityRole), role));
        }

        public Task<ProjectsGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal)
        {
            return GetPrimaryGroupByIdAsync(GetPrimaryGroupId(principal));
        }

        public Task<ProjectsGroup> GetPrimaryGroupByIdAsync(string groupId)
        {
            return _dbContext.Groups.FindAsync(groupId);
        }

        public Task<bool> GroupExistsAsync(string groupId)
        {
            return _dbContext.Groups.AnyAsync(g => g.Id == groupId);
        }

        public IQueryable<ProjectsGroup> QueryGroups()
        {
            return _dbContext.Groups;
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

        public Task<bool> UserExistsAsync(string userId)
        {
            return _dbContext.Users.AnyAsync(u => u.Id == userId);
        }

        public IQueryable<ProjectsUser> QueryUsers()
        {
            return _dbContext.Users;
        }
    }

    public interface IUserManager
    {
        string GetUserId(ClaimsPrincipal principal);
        string GetUserName(ClaimsPrincipal principal);
        string GetEmailAddress(ClaimsPrincipal principal);
        string GetPrimaryGroupId(ClaimsPrincipal principal);
        IEnumerable<ApprovalAuthorityRole> GetRoles(ClaimsPrincipal principal);
        bool IsInRole(ClaimsPrincipal principal, ApprovalAuthorityRole role);

        Task<ProjectsGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal);
        Task<ProjectsGroup> GetPrimaryGroupByIdAsync(string groupId);
        Task<bool> GroupExistsAsync(string groupId);
        IQueryable<ProjectsGroup> QueryGroups();

        Task<ProjectsUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup = false);
        Task<ProjectsUser> GetUserByIdAsync(string userId, bool includeGroup = false);
        Task<bool> UserExistsAsync(string userId);
        IQueryable<ProjectsUser> QueryUsers();
    }
}