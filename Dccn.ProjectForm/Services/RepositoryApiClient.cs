using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using Dccn.ProjectForm.DataTransferObjects;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class RepositoryApiClient : IRepositoryApiClient
    {
        private readonly HttpClient _client;

        public RepositoryApiClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<RepositoryUserDto> FindUserByEmailAddressAsync(string email)
        {
            var uri = $"users/query?email={HttpUtility.UrlEncode(email)}&detail";

            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsAsync<ApiResult<RepositoryUserDto[]>>();
            if (!result.Success)
            {
                throw new ApplicationException($"Repository API Error. Message: {result.ErrorMessage}. Code: {result.ErrorCode}");
            }

            return result.Data.SingleOrDefault();
        }

        private class ApiResult<TData>
        {
            [DataMember(Name="ec")]
            public int ErrorCode { get; set; }

            [DataMember(Name="errmsg")]
            public string ErrorMessage { get; set; }

            [DataMember(Name="data")]
            public TData Data { get; set; }

            public bool Success => ErrorCode == 0;
        }
    }

    public interface IRepositoryApiClient
    {
        Task<RepositoryUserDto> FindUserByEmailAddressAsync(string email);
    }
}
