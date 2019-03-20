using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dccn.ProjectForm.Services;

namespace Dccn.ProjectForm.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserManager _userManager;
        private readonly IRepositoryApiClient _repositoryApiClient;

        public UsersController(IUserManager userManager, IRepositoryApiClient repositoryApiClient)
        {
            _userManager = userManager;
            _repositoryApiClient = repositoryApiClient;
        }

        [HttpGet]
        [ActionName("Query")]
        public async Task<ActionResult<IEnumerable<UserDto>>> QueryAsync([Required] string query, [Range(1, 25)] int limit = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            query = query.Trim();
            if (query.Length < 2)
            {
                return new ActionResult<IEnumerable<UserDto>>(Enumerable.Empty<UserDto>());
            }

            var queryResult = await _userManager.QueryUsers()
                .Where(u => u.Status == CheckinStatus.CheckedIn || u.Status == CheckinStatus.CheckedOutExtended)
                .Where(u => (u.FirstName + " " + (string.IsNullOrEmpty(u.MiddleName) ? string.Empty : u.MiddleName + " ") + u.LastName).Contains(query) || u.Id.StartsWith(query))
                .ToListAsync();

            return queryResult
                .OrderBy(u => u.DisplayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase))
                .ThenBy(u => u.DisplayName)
                .Take(limit)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.DisplayName,
                    Email = u.Email
                })
                .ToList();
        }

        [HttpGet]
        [ActionName("RepositoryUserExists")]
        public async Task<ActionResult<bool>> RepositoryUserExistsAsync(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserByIdAsync(id);
            if (user == null)
            {
                return true;
            }

            var repoUser = await _repositoryApiClient.FindUserByEmailAddressAsync(user.Email);
            if (repoUser == null)
            {
                return false;
            }

            return NoContent();
        }
    }
}