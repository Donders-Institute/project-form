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
using LinqKit;

namespace Dccn.ProjectForm.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UsersController : ControllerBase
    {
        private const int QueryPartMinLength = 2;
        private const int QueryPartCountLimit = 5;
        private const int QueryResultCountMaxLimit = 25;
        private const int QueryResultCountDefaultLimit = 10;

        private readonly IUserManager _userManager;
        private readonly IRepositoryApiClient _repositoryApiClient;

        public UsersController(IUserManager userManager, IRepositoryApiClient repositoryApiClient)
        {
            _userManager = userManager;
            _repositoryApiClient = repositoryApiClient;
        }

        [HttpGet]
        [ActionName("Query")]
        public async Task<ActionResult<IEnumerable<UserDto>>> QueryAsync([Required] string query, [Range(1, QueryResultCountMaxLimit)] int limit = QueryResultCountDefaultLimit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var queryParts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(QueryPartCountLimit).ToList();
            if (!queryParts.Any(p => p.Length >= QueryPartMinLength))
            {
                return new ActionResult<IEnumerable<UserDto>>(Enumerable.Empty<UserDto>());
            }

            var match = queryParts
                .Aggregate(PredicateBuilder.New<ProjectDbUser>(false), (pred, part) =>
                    pred.And(user =>
                        user.FirstName.Contains(part) || user.MiddleName.Contains(part) ||
                        user.LastName.Contains(part) || user.Id.StartsWith(part)));

            var queryResult = await _userManager.Users
                .Where(u => u.Status == CheckinStatus.CheckedIn || u.Status == CheckinStatus.CheckedOutExtended)
                .Where(match)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Email
                })
                .ToListAsync();

            var comparer = new IndexOfComparer(string.Join(' ', queryParts), StringComparison.CurrentCultureIgnoreCase);
            return queryResult
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = ProjectDbUser.GetDisplayName(u.Id, u.FirstName, u.MiddleName, u.LastName)
                })
                .OrderBy(u => u.Name, comparer)
                .ThenBy(u => u.Name)
                .Take(limit)
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
                return false;
            }

            var repoUsers = await _repositoryApiClient.FindUsersByEmailAddressAsync(user.Email);
            return repoUsers.Any();
        }

        private class IndexOfComparer : Comparer<string>
        {
            private readonly string _substring;
            private readonly StringComparison _comparison;

            public IndexOfComparer(string substring, StringComparison comparison)
            {
                _substring = substring;
                _comparison = comparison;
            }

            public override int Compare(string x, string y)
            {
                unchecked
                {
                    var rankX = (uint) (x?.IndexOf(_substring, _comparison) ?? -1);
                    var rankY = (uint) (y?.IndexOf(_substring, _comparison) ?? -1);
                    return rankX.CompareTo(rankY);
                }
            }
        }
    }
}