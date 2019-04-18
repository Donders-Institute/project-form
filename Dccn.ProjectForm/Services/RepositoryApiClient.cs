using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.DataTransferObjects;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class RepositoryApiClient : IRepositoryApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public RepositoryApiClient(HttpClient client, ILogger<RepositoryApiClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<IEnumerable<RepositoryUserDto>> FindUsersByEmailAddressAsync(string email)
        {
            var uri = $"users/query?email={HttpUtility.UrlEncode(email)}&detail";

            try
            {
                var response = await _client.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsAsync<ApiResult<RepositoryUserDto[]>>();
                if (!result.Succeeded)
                {
                    _logger.LogError($"Repository API Error. Message: {result.ErrorMessage}. Code: {result.ErrorCode}");
                    return Enumerable.Empty<RepositoryUserDto>();
                }

                return result.Data;
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("Error contacting repository API.", e);
                return Enumerable.Empty<RepositoryUserDto>();
            }
        }

        private class ApiResult<TData>
        {
            [DataMember(Name = "ec")]
            public int ErrorCode { get; set; }

            [DataMember(Name = "errmsg")]
            public string ErrorMessage { get; set; }

            [DataMember(Name = "data")]
            public TData Data { get; set; }

            public bool Succeeded => ErrorCode == 0;
        }
    }

    public interface IRepositoryApiClient
    {
        Task<IEnumerable<RepositoryUserDto>> FindUsersByEmailAddressAsync(string email);
    }

    public static class RespositoryApiClientExtensions
    {
        public static IServiceCollection AddRepositoryApiClient(this IServiceCollection collection)
        {
            collection.AddHttpClient<IRepositoryApiClient, RepositoryApiClient>((services, client) =>
            {
                var options = services.GetService<IOptions<RepositoryApiOptions>>()?.Value;
                if (options == null)
                {
                    return;
                }

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.UserName}:{options.Password}"));

                client.BaseAddress = new Uri(options.BaseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            });

            return collection;
        }
    }
}
