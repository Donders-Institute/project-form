using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Dccn.ProjectForm.Pages
{
    [Authorize(Roles = nameof(Role.Supervisor))]
    public class SupervisedProposalsModel : ProposalsPageModel
    {
        public SupervisedProposalsModel(ProposalsDbContext proposalsDbContext, IUserManager userManager) : base(proposalsDbContext, userManager)
        {
        }

        [UsedImplicitly]
        public async Task OnGetAsync()
        {
            var userId = UserManager.GetUserId(User);
            await LoadProposalsAsync(query => query.Where(p => p.SupervisorId == userId));
        }
    }
}