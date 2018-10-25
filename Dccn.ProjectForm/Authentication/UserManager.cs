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

        public string GetUserEmail(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.EmailAddress).Value;
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
        string GetUserEmail(ClaimsPrincipal principal);
        Task<ProjectsUser> GetUserAsync(ClaimsPrincipal principal, bool includeGroup);
        Task<ProjectsUser> GetUserByIdAsync(string userId, bool includeGroup);
    }
}