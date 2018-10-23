using System.Threading.Tasks;
using Dccn.ProjectForm.Data.Projects;

namespace Dccn.ProjectForm.Data
{
    public class UserReference
    {
        public string Id { get; private set; }
        public string DisplayName { get; set; }

        public static async Task<UserReference> FromExistingAsync(string id, ProjectsDbContext dbContext)
        {
            var user = await dbContext.Users.FindAsync(id);
            return new UserReference
            {
                Id = user.Id,
                DisplayName = user.DisplayName
            };
        }
    }
}
