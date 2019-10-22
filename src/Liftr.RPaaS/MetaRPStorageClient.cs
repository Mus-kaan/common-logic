//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.Contracts.Exceptions;
using Microsoft.Liftr.TokenManager;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS
{
    public class MetaRPStorageClient : IMetaRPStorageClient
    {
        private readonly RPaaSConfiguration _configuration;
        private readonly ITokenManager _tokenManager;
        private readonly HttpClient _httpClient;

        public MetaRPStorageClient(IOptions<RPaaSConfiguration> configuration, ITokenManager tokenManager, HttpClient httpClient)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration.Value;
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(_configuration.MetaRPEndpoint);
        }

        public async Task<T> GetResourceAsync<T>(string resourceId, string apiVersion) where T : ARMResource
        {
            var url = GetMetaraRPResourceUrl(resourceId, apiVersion);
            _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(
                    await response.Content.ReadAsStringAsync());
            }
            else
            {
                throw HttpResponseException.Create(response, resourceId);
            }
        }

        public async Task UpdateResourceAsync<T>(T resource, string apiVersion) where T : ARMResource
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            using (var content = new StringContent(JsonConvert.SerializeObject(resource), Encoding.UTF8, "application/json"))
            {
                var url = GetMetaraRPResourceUrl(resource.Id, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw HttpResponseException.Create(response, resource.Id);
                }
            }
        }

        private string GetMetaraRPResourceUrl(string resourceId, string apiVersion)
        {
            return resourceId + "?api-version=" + apiVersion;
        }

        private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync()
        {
            return new AuthenticationHeaderValue(
                        "Bearer",
                        await _tokenManager.GetTokenAsync(_configuration.MetaRPAccessorClientId, _configuration.MetaRPAccessorClientSecret));
        }
    }
}
