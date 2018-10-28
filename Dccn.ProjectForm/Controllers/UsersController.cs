using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dccn.ProjectForm.Models;
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
        public async Task<ActionResult<ICollection<UserModel>>> QueryAsync([Required, MinLength(2)] string query, [Range(1, 25)] int limit = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // FIXME: inefficient query
            return await _userManager.QueryUsers()
                .Select(u => new {Index = u.DisplayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase), User = u})
                .Where(u => u.Index >= 0 || u.User.Id.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(u => u.Index)
                .ThenBy(u => u.User.DisplayName)
                .Take(limit)
                .Select(u => new UserModel {Id = u.User.Id, Name = u.User.DisplayName})
                .ToListAsync();
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