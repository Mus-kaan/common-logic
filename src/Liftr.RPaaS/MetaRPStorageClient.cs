//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS
{
#nullable enable
    public class MetaRPStorageClient : IMetaRPStorageClient
    {
        private readonly HttpClient _httpClient;
        private readonly MetaRPOptions _options;
        private readonly AuthenticationTokenCallback _tokenCallback;
        private readonly Serilog.ILogger _logger;

        private static readonly JsonSerializerSettings s_camelCaseSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
        };

        public MetaRPStorageClient(
            HttpClient httpClient,
            MetaRPOptions metaRpOptions,
            AuthenticationTokenCallback tokenCallback,
            Serilog.ILogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = metaRpOptions ?? throw new ArgumentNullException(nameof(metaRpOptions));
            _httpClient.BaseAddress = _options.MetaRPEndpoint ?? throw new InvalidOperationException($"{nameof(_options.MetaRPEndpoint)} cannot be null");
            _tokenCallback = tokenCallback ?? throw new ArgumentNullException(nameof(tokenCallback));
            _logger = logger ?? throw new ArgumentNullException(nameof(tokenCallback));

            logger.Information($"Loading token to make sure '{nameof(MetaRPStorageClient)}' is correctly initialized");
            try
            {
                var token = tokenCallback(_options.UserRPTenantId).GetAwaiter().GetResult();
                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidOperationException($"Cannot load token for {nameof(MetaRPStorageClient)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Cannot acquire FPA token for {nameof(MetaRPStorageClient)}");
                throw;
            }
        }

        public delegate Task<string> AuthenticationTokenCallback(string tenantId);

        #region Resource operations

        /// <inheritdoc/>
        public async Task<T> GetResourceAsync<T>(string resourceId, string tenantId, string apiVersion)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Please provide the User's tenant id", nameof(tenantId));
            }

            using var operation = _logger.StartTimedOperation(nameof(GetResourceAsync));
            operation.SetContextProperty(nameof(resourceId), resourceId);
            operation.SetContextProperty(nameof(tenantId), tenantId);
            var url = Utils.GetMetaRPResourceUrl(resourceId, apiVersion);
            var request = await CreateRequestAsync(HttpMethod.Get, url, tenantId);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(
                    await response.Content.ReadAsStringAsync());
            }
            else
            {
                var errorMessage = $"Failed at getting resource from RPaaS. StatusCode: '{response.StatusCode}'";
                if (response.Content != null)
                {
                    errorMessage = errorMessage + $", Response: '{await response.Content.ReadAsStringAsync()}'";
                }

                _logger.LogError(errorMessage);
                operation.FailOperation(response.StatusCode, errorMessage);
                throw MetaRPException.Create(response, nameof(GetResourceAsync));
            }
        }

        public async Task<HttpResponseMessage> DeleteResourceAsync<T>(string resourceId, string tenantId, string apiVersion)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Please provide the User's tenant id", nameof(tenantId));
            }

            using (var operation = _logger.StartTimedOperation(nameof(DeleteResourceAsync)))
            {
                operation.SetContextProperty(nameof(resourceId), resourceId);
                operation.SetContextProperty(nameof(tenantId), tenantId);

                var url = Utils.GetMetaRPResourceUrl(resourceId, apiVersion);
                var request = await CreateRequestAsync(HttpMethod.Delete, url, tenantId);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Failed at deleting resource from RPaaS. StatusCode: '{response.StatusCode}'";
                    if (response.Content != null)
                    {
                        errorMessage = errorMessage + $", Response: '{await response.Content.ReadAsStringAsync()}'";
                    }

                    _logger.LogError(errorMessage);
                    operation.FailOperation(response.StatusCode, errorMessage);
                    throw MetaRPException.Create(response, nameof(DeleteResourceAsync));
                }

                return response;
            }
        }

        public async Task<HttpResponseMessage> PutResourceAsync<T>(T resource, string resourceId, string tenantId, string apiVersion)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Please provide the User's tenant id", nameof(tenantId));
            }

            using (var operation = _logger.StartTimedOperation(nameof(PutResourceAsync)))
            using (var content = new StringContent(JsonConvert.SerializeObject(resource, s_camelCaseSettings), Encoding.UTF8, "application/json"))
            {
                operation.SetContextProperty(nameof(resourceId), resourceId);
                operation.SetContextProperty(nameof(tenantId), tenantId);

                var url = Utils.GetMetaRPResourceUrl(resourceId, apiVersion);
                var request = await CreateRequestAsync(HttpMethod.Put, url, tenantId, content);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Failed at updating resource from RPaaS. StatusCode: '{response.StatusCode}'";
                    if (response.Content != null)
                    {
                        errorMessage = errorMessage + $", Response: '{await response.Content.ReadAsStringAsync()}'";
                    }

                    _logger.LogError(errorMessage);
                    operation.FailOperation(response.StatusCode, errorMessage);
                    throw MetaRPException.Create(response, nameof(PutResourceAsync));
                }

                return response;
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> PatchResourceAsync<T>(T resource, string resourceId, string tenantId, string apiVersion)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Please provide the User's tenant id", nameof(tenantId));
            }

            using (var operation = _logger.StartTimedOperation(nameof(PatchResourceAsync)))
            using (var content = new StringContent(JsonConvert.SerializeObject(resource, s_camelCaseSettings), Encoding.UTF8, "application/json"))
            {
                operation.SetContextProperty(nameof(resourceId), resourceId);
                operation.SetContextProperty(nameof(tenantId), tenantId);
                var method = new HttpMethod("PATCH");

                // For patch operation we need to retry on 404 as sometimes due to ARM cache replication issue, we get 404 on first attempt
                var retryPolicy = HttpPolicies.GetRetryPolicyForNotFound(_logger);
                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    var url = Utils.GetMetaRPResourceUrl(resourceId, apiVersion);
                    var request = await CreateRequestAsync(method, url, tenantId, content);

                    return await _httpClient.SendAsync(request);
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Failed at updating resource from RPaaS. StatusCode: '{response.StatusCode}'";
                    if (response.Content != null)
                    {
                        errorMessage = errorMessage + $", Response: '{await response.Content.ReadAsStringAsync()}'";
                    }

                    _logger.LogError(errorMessage);
                    operation.FailOperation(response.StatusCode, errorMessage);
                    throw MetaRPException.Create(response, nameof(PatchResourceAsync));
                }

                return response;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> ListResourcesAsync<T>(string resourcePath, string apiVersion)
        {
            var listResponse = new ListResponse<T>()
            {
                Value = new List<T>(),
                NextLink = Utils.GetMetaRPResourceUrl(resourcePath, apiVersion) + "&$expand=crossPartitionQuery",
            };

            return await ListMetaRPResourcesAsync(resourcePath, listResponse);
        }

        public async Task<IEnumerable<T>> ListFilteredResourcesAsync<T>(string resourcePath, string apiVersion, string filterCondition)
        {
            if (string.IsNullOrEmpty(filterCondition))
            {
                throw new ArgumentNullException(nameof(filterCondition));
            }

            var listResponse = new ListResponse<T>()
            {
                Value = new List<T>(),
                NextLink = Utils.GetMetaRPResourceUrl(resourcePath, apiVersion) + "&$filter=" + filterCondition + "&$expand=crossPartitionQuery",
            };

            return await ListMetaRPResourcesAsync(resourcePath, listResponse);
        }
        #endregion

        #region Subscription operations

        /// <inheritdoc/>
        public async Task<string> GetTenantForSubscriptionAsync(
            string userRpSubscriptionId, string providerName, string subscriptionId, string apiVersion)
        {
            var resourceId = $"/subscriptions/{userRpSubscriptionId}/providers/{providerName}/registeredSubscriptions/{subscriptionId}";
            var subscription = await GetResourceAsync<RegisteredSubscriptionModel>(resourceId, _options.UserRPTenantId, apiVersion);
            return subscription.TenantId;
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, string>> GetTenantForAllSubscriptionsAsync(
            string userRpSubscriptionId, string providerName, string apiVersion)
        {
            var resourceId = $"/subscriptions/{userRpSubscriptionId}/providers/{providerName}/registeredSubscriptions";
            var registeredSubscriptions = await ListResourcesAsync<RegisteredSubscriptionModel>(resourceId, apiVersion);
            var dictionary = new Dictionary<string, string>();

            foreach (var registration in registeredSubscriptions)
            {
                dictionary[registration.SubscriptionId] = registration.TenantId;
            }

            return dictionary;
        }

        #endregion

        #region Operation operations

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> PatchOperationAsync<T>(T operation, string tenantId, string apiVersion) where T : OperationResource
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Please provide the User's tenant id", nameof(tenantId));
            }

            using var ops = _logger.StartTimedOperation(nameof(PatchOperationAsync));
            using var content = new StringContent(JsonConvert.SerializeObject(operation, s_camelCaseSettings), Encoding.UTF8, "application/json");
            ops.SetContextProperty("AsyncOperationId", operation.Id);

            var url = Utils.GetMetaRPResourceUrl(operation.Id, apiVersion);
            var request = await CreateRequestAsync(new HttpMethod("PATCH"), url, tenantId, content);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"Failed at patching operation status. StatusCode: '{response.StatusCode}'";
                if (response.Content != null)
                {
                    errorMessage = errorMessage + $", Response: '{await response.Content.ReadAsStringAsync()}'";
                }

                _logger.LogError(errorMessage);
                ops.FailOperation(response.StatusCode, errorMessage);
                throw MetaRPException.Create(response, nameof(PatchOperationAsync));
            }

            return response;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> PatchOperationStatusAsync(
            string operationStatusId,
            ProvisioningState state,
            string tenantId,
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
            return await PatchOperationAsync(operation, tenantId, apiVersion);
        }

        #endregion

        private async Task<IEnumerable<T>> ListMetaRPResourcesAsync<T>(string resourcePath, ListResponse<T> listResponse)
        {
            var resources = new List<T>();

            using (var operation = _logger.StartTimedOperation(nameof(ListMetaRPResourcesAsync)))
            {
                operation.SetContextProperty(nameof(resourcePath), resourcePath);
                do
                {
                    var request = await CreateRequestAsync(HttpMethod.Get, listResponse.NextLink, _options.UserRPTenantId);
                    var response = await _httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMessage = $"Failed at listing resources. StatusCode: '{response.StatusCode}'";
                        if (response.Content != null)
                        {
                            errorMessage = errorMessage + $", Response: '{await response.Content.ReadAsStringAsync()}'";
                        }

                        _logger.LogError(errorMessage);
                        operation.FailOperation(response.StatusCode, errorMessage);
                        throw MetaRPException.Create(response, nameof(ListMetaRPResourcesAsync));
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    listResponse = content.FromJson<ListResponse<T>>();
                    resources.AddRange(listResponse.Value);
                }
                while (listResponse.NextLink != null);
            }

            return resources;
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod httpMethod, string url, string tenantId, StringContent? content = null)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException($"'{nameof(tenantId)}' cannot be null or empty.", nameof(tenantId));
            }

            var accessToken = await _tokenCallback(tenantId);
            var request = new HttpRequestMessage(httpMethod, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            if (content != null)
            {
                request.Content = content;
            }

            return request;
        }
    }
}
