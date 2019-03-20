using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Controllers
{
    [Route("Project")]
    public class ProjectController : Controller
    {
        private readonly ProposalDbContext _proposalDbContext;

        public ProjectController(ProposalDbContext proposalDbContext)
        {
            _proposalDbContext = proposalDbContext;
        }

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetProjectAsync(string projectId)
        {
            var proposal = await _proposalDbContext.Proposals.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (proposal == null)
            {
                return NotFound();
            }

            return RedirectToPagePermanent("/Form", new { proposalId = proposal.Id });
        }
    }
}