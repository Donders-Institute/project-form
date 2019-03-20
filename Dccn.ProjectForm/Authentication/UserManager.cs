using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Authentication
{
    public class UserManager : IUserManager
    {
        private readonly ProjectDbContext _dbContext;

        public UserManager(ProjectDbContext dbContext)
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

        public IEnumerable<ApprovalAuthorityRole> GetApprovalRoles(ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.ApprovalRole).Select(r => Enum.Parse<ApprovalAuthorityRole>(r.Value));
        }

        public bool IsInApprovalRole(ClaimsPrincipal principal, ApprovalAuthorityRole role)
        {
            return GetApprovalRoles(principal).Contains(role);
        }

        public IEnumerable<Role> GetRoles(ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role).Select(r => Enum.Parse<Role>(r.Value));
        }

        public bool IsInRole(ClaimsPrincipal principal, Role role)
        {
            return principal.IsInRole(Enum.GetName(typeof(Role), role));
        }

        public Task<ProjectDbGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal)
        {
            return GetPrimaryGroupByIdAsync(GetPrimaryGroupId(principal));
        }

        public Task<ProjectDbGroup> GetPrimaryGroupByIdAsync(string groupId)
        {
            return _dbContext.Groups.FindAsync(groupId);
        }

        public Task<bool> GroupExistsAsync(string groupId)
        {
            return _dbContext.Groups.AnyAsync(g => g.Id == groupId);
        }

        public IQueryable<ProjectDbGroup> QueryGroups()
        {
            return _dbContext.Groups;
        }

        public Task<ProjectDbUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup)
        {
            return GetUserByIdAsync(GetUserId(principal), includeGroup);
        }

        public async Task<ProjectDbUser> GetUserByIdAsync(string userId, bool includeGroup)
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

        public IQueryable<ProjectDbUser> QueryUsers()
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
        IEnumerable<Role> GetRoles(ClaimsPrincipal principal);
        bool IsInRole(ClaimsPrincipal principal, Role role);
        IEnumerable<ApprovalAuthorityRole> GetApprovalRoles(ClaimsPrincipal principal);
        bool IsInApprovalRole(ClaimsPrincipal principal, ApprovalAuthorityRole role);

        Task<ProjectDbGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal);
        Task<ProjectDbGroup> GetPrimaryGroupByIdAsync(string groupId);
        Task<bool> GroupExistsAsync(string groupId);
        IQueryable<ProjectDbGroup> QueryGroups();

        Task<ProjectDbUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup = false);
        Task<ProjectDbUser> GetUserByIdAsync(string userId, bool includeGroup = false);
        Task<bool> UserExistsAsync(string userId);
        IQueryable<ProjectDbUser> QueryUsers();
    }
}