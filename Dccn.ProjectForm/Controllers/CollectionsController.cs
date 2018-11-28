using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Controllers
{
    [ApiController]
    public class CollectionsController : ControllerBase
    {
        private readonly ProposalsDbContext _proposalsDbContext;
        private readonly IUserManager _userManager;
        private readonly IAuthorizationService _authorizationService;

        public CollectionsController(ProposalsDbContext proposalsDbContext, IUserManager userManager, IAuthorizationService authorizationService)
        {
            _proposalsDbContext = proposalsDbContext;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }

        [HttpPost("/Experimenters/Add/{proposalId}", Name = "AddExperimenter")]
        public async Task<ActionResult<Experimenter>> AddExperimenterAsync(int proposalId, [Required] byte[] timestamp, [Required] string userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var proposal = await _proposalsDbContext.Proposals
                .Include(p => p.Approvals)
                .Include(p => p.Experimenters)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null)
            {
                return NotFound();
            }

            if (!(await _authorizationService.AuthorizeAsync(User, proposal, FormSectionOperation.Edit<ExperimentSectionModel>())).Succeeded)
            {
                return Forbid();
            }

            if (!proposal.Timestamp.SequenceEqual(timestamp))
            {
                return Conflict();
            }

            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError(nameof(userId), "User with the given ID does not exist.");
                return BadRequest(ModelState);
            }

            if (proposal.Experimenters.Any(e => e.UserId == user.Id))
            {
                ModelState.AddModelError(nameof(userId), "Experimenter already in list.");
                return BadRequest(ModelState);
            }

            var experimenter = new Experimenter
            {
                UserId = user.Id
            };

            proposal.Experimenters.Add(experimenter);

            await _proposalsDbContext.SaveChangesAsync();

            return experimenter;
        }
    }
}