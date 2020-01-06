﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.Contracts.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS
{
    public class MetaRPStorageClient : IMetaRPStorageClient
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationTokenCallback _tokenCallback;

        public MetaRPStorageClient(
            string metaRPEndpoint,
            HttpClient httpClient,
            AuthenticationTokenCallback tokenCallback)
        {
            if (string.IsNullOrEmpty(metaRPEndpoint))
            {
                throw new ArgumentNullException(nameof(metaRPEndpoint));
            }

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(metaRPEndpoint);
            _tokenCallback = tokenCallback ?? throw new ArgumentNullException(nameof(tokenCallback));
        }

        public delegate Task<string> AuthenticationTokenCallback();

        public async Task<T> GetResourceAsync<T>(string resourceId, string apiVersion) where T : ARMResource
        {
            var url = GetMetaRPResourceUrl(resourceId, apiVersion);
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

        public async Task<HttpResponseMessage> UpdateResourceAsync<T>(T resource, string apiVersion) where T : ARMResource
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            using (var content = new StringContent(JsonConvert.SerializeObject(resource), Encoding.UTF8, "application/json"))
            {
                var url = GetMetaRPResourceUrl(resource.Id, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
                var response = await _httpClient.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw HttpResponseException.Create(response, resource.Id);
                }

                return response;
            }
        }

        public async Task<IEnumerable<T>> ListAllResourcesOfTypeAsync<T>(Guid userRpSubscriptionId, string providerNamespace, string resourceType, string apiVersion) where T : ARMResource
        {
            // ToDo: Implement paging support here
            var url = $"/subscriptions/{userRpSubscriptionId.ToString()}/providers/{providerNamespace}/{resourceType}?api-version={apiVersion}&$expand=crossPartitionQuery";
            _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw HttpResponseException.Create(response, nameof(ListAllResourcesOfTypeAsync));
            }

            return (await response.Content.ReadAsStringAsync()).FromJson<ListResponse<T>>().Value;
        }

        public async Task<HttpResponseMessage> PatchOperationAsync<T>(T operation, string apiVersion) where T : OperationResource
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            using (var content = new StringContent(JsonConvert.SerializeObject(operation), Encoding.UTF8, "application/json"))
            {
                var url = GetMetaRPResourceUrl(operation.Id, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
                var method = new HttpMethod("PATCH");
                using (var request = new HttpRequestMessage(method, url)
                {
                    Content = content,
                })
                {
                    var response = await _httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw HttpResponseException.Create(response, operation.Id);
                    }

                    return response;
                }
            }
        }

        public async Task<HttpResponseMessage> PatchOperationStatusAsync(
            string operationStatusId,
            ProvisioningState state,
            string errorCode,
            string errorMessage,
            string apiVersion)
        {
            if (string.IsNullOrEmpty(operationStatusId))
            {
                throw new ArgumentNullException(nameof(operationStatusId));
            }

            var operation = new OperationResource()
            {
                Id = operationStatusId,
                Status = state,
                Error = new OperationError()
                {
                    Code = errorCode,
                    Message = errorMessage,
                },
            };
            return await PatchOperationAsync(operation, apiVersion);
        }

        private static string GetMetaRPResourceUrl(string resourceId, string apiVersion)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                return resourceId;
            }

            int index = resourceId.IndexOf("?api-version=", StringComparison.CurrentCultureIgnoreCase);
            if (index >= 0)
            {
                return resourceId.Substring(0, index) + "?api-version=" + apiVersion;
            }

            return resourceId + "?api-version=" + apiVersion;
        }

        private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync()
        {
            var authenticationHeader = new AuthenticationHeaderValue(
                        "Bearer",
                        await _tokenCallback());
            return authenticationHeader;
        }
    }

    public class ListResponse<T> where T : ARMResource
    {
        public List<T> Value { get; set; }
    }
}
