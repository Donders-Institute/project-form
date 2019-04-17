using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
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

        public Task<ProjectDbGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        {
            return GetPrimaryGroupByIdAsync(GetPrimaryGroupId(principal), cancellationToken);
        }

        public Task<ProjectDbGroup> GetPrimaryGroupByIdAsync(string groupId, CancellationToken cancellationToken)
        {
            return Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        }

        public Task<bool> GroupExistsAsync(string groupId, CancellationToken cancellationToken)
        {
            return Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
        }

        public IQueryable<ProjectDbGroup> Groups => _dbContext.Groups;//.AsNoTracking();

        public Task<ProjectDbUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup, CancellationToken cancellationToken)
        {
            return GetUserByIdAsync(GetUserId(principal), includeGroup, cancellationToken);
        }

        public async Task<ProjectDbUser> GetUserByIdAsync(string userId, bool includeGroup, CancellationToken cancellationToken)
        {
            var query = includeGroup ? Users.Include(u => u.Group) : Users;
            return await query.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<ICollection<ProjectDbUser>> GetUsersByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
        {
            var users = await Users
                .Where(u => userIds.Distinct().Contains(u.Id))
                .ToListAsync(cancellationToken);

            return userIds
                .Join(users, id => id, user => user.Id, (id, user) => user)
                .ToList();
        }

        public async Task<IDictionary<string, string>> GetUserNamesForIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            return await Users
                .Where(u => userIds.Distinct().Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName
                })
                .ToDictionaryAsync(
                    u => u.Id,
                    u => ProjectDbUser.GetDisplayName(u.Id, u.FirstName, u.MiddleName, u.LastName),
                    cancellationToken);
        }

        public Task<bool> UserExistsAsync(string userId)
        {
            return Users.AnyAsync(u => u.Id == userId);
        }

        public IQueryable<ProjectDbUser> Users => _dbContext.Users; //.AsNoTracking();
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

        Task<ProjectDbGroup> GetPrimaryGroupAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
        Task<ProjectDbGroup> GetPrimaryGroupByIdAsync(string groupId, CancellationToken cancellationToken = default);
        Task<bool> GroupExistsAsync(string groupId, CancellationToken cancellationToken = default);
        IQueryable<ProjectDbGroup> Groups { get; }

        Task<ProjectDbUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup = false, CancellationToken cancellationToken = default);
        Task<ProjectDbUser> GetUserByIdAsync(string userId, bool includeGroup = false, CancellationToken cancellationToken = default);
        Task<ICollection<ProjectDbUser>> GetUsersByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
        Task<IDictionary<string, string>> GetUserNamesForIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
        Task<bool> UserExistsAsync(string userId);
        IQueryable<ProjectDbUser> Users { get; }
    }
}