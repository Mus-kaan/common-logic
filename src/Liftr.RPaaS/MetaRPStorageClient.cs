//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.ARM;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS
{
    public class MetaRPStorageClient : IMetaRPStorageClient
    {
        private const string MetricTypeHeaderKey = "x-ms-metrictype";
        private const string MetricTypeHeaderValue = "metarp";
        private const double FirstRetryDelay = 3;
        private const int RetryCount = 5;

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
            Uri metaRPEndpoint,
            HttpClient httpClient,
            MetaRPOptions metaRpOptions,
            AuthenticationTokenCallback tokenCallback,
            Serilog.ILogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = metaRpOptions ?? throw new ArgumentNullException(nameof(metaRpOptions));
            _httpClient.BaseAddress = metaRPEndpoint ?? throw new ArgumentNullException(nameof(metaRPEndpoint));
            _tokenCallback = tokenCallback ?? throw new ArgumentNullException(nameof(tokenCallback));
            _logger = logger ?? throw new ArgumentNullException(nameof(tokenCallback));

            logger.Information($"Loading token to make sure '{nameof(MetaRPStorageClient)}' is correctly initialized");
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            var token = tokenCallback(_options.UserRPTenantId).Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException($"Cannot load token for {nameof(MetaRPStorageClient)}");
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

            using (var operation = _logger.StartTimedOperation(nameof(GetResourceAsync)))
            {
                operation.SetContextProperty(nameof(resourceId), resourceId);
                operation.SetContextProperty(nameof(tenantId), tenantId);
                var url = GetMetaRPResourceUrl(resourceId, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync(tenantId);
                _httpClient.DefaultRequestHeaders.Add(MetricTypeHeaderKey, MetricTypeHeaderValue);

                var response = await _httpClient.GetAsync(url);
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
                var url = GetMetaRPResourceUrl(resourceId, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync(tenantId);
                _httpClient.DefaultRequestHeaders.Add(MetricTypeHeaderKey, MetricTypeHeaderValue);

                var response = await _httpClient.PutAsync(url, content);
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
                var url = GetMetaRPResourceUrl(resourceId, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync(tenantId);
                _httpClient.DefaultRequestHeaders.Add(MetricTypeHeaderKey, MetricTypeHeaderValue);

                var method = new HttpMethod("PATCH");

                // For patch operation we need to retry on 404 as sometimes due to ARM cache replication issue, we get 404 on first attempt
                var retryPolicy = GetRetryPolicyForNotFound();
                var response = await retryPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(method, url)
                    {
                        Content = content,
                    };

                    return _httpClient.SendAsync(request);
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
                NextLink = GetMetaRPResourceUrl(resourcePath, apiVersion) + "&$expand=crossPartitionQuery",
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
                NextLink = GetMetaRPResourceUrl(resourcePath, apiVersion) + "&$filter=" + filterCondition + "&$expand=crossPartitionQuery",
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
            var url = GetMetaRPResourceUrl(operation.Id, apiVersion);
            _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync(tenantId);
            _httpClient.DefaultRequestHeaders.Add(MetricTypeHeaderKey, MetricTypeHeaderValue);

            var method = new HttpMethod("PATCH");
            using (var request = new HttpRequestMessage(method, url)
            {
                Content = content,
            })
            {
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

        private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant Id cannot be empty", nameof(tenantId));
            }

            var authenticationHeader = new AuthenticationHeaderValue(
                        "Bearer",
                        await _tokenCallback(tenantId));
            return authenticationHeader;
        }

        /// <summary>
        /// For patch operation we need to retry on 404 as sometimes due to ARM cache replication issue, we get 404 on first attempt
        /// </summary>
        private AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyForNotFound()
        {
            var delay = GetJitteredBackoffDelay();

            HttpStatusCode[] httpStatusCodesWorthRetrying =
           {
                   HttpStatusCode.NotFound, // 404
           };

            return GetPolicy(httpStatusCodesWorthRetrying, delay);
        }

        private AsyncRetryPolicy<HttpResponseMessage> GetPolicy(HttpStatusCode[] httpStatusCodesWorthRetrying, IEnumerable<TimeSpan> delay)
        {
            return Policy
                  .HandleResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                  .WaitAndRetryAsync(
                      delay,
                      onRetry: (outcome, timespan, retryAttempt, context) =>
                      {
                          LogRetryInfo(outcome, timespan, retryAttempt);
                      });
        }

        /// <summary>
        /// Reference: https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation
        /// </summary>
        private static IEnumerable<TimeSpan> GetJitteredBackoffDelay() => Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(FirstRetryDelay), retryCount: RetryCount);

        private void LogRetryInfo(DelegateResult<HttpResponseMessage> outcome, TimeSpan timespan, int retryAttempt)
        {
            _logger.Information("Request: {requestMethod} {requestUrl} failed. Delaying for {delay}ms, then retrying attempt is: {retry} / {totalCount}.", outcome.Result.RequestMessage?.Method, outcome.Result.RequestMessage?.RequestUri, timespan.TotalMilliseconds, retryAttempt, RetryCount);
        }

        private async Task<IEnumerable<T>> ListMetaRPResourcesAsync<T>(string resourcePath, ListResponse<T> listResponse)
        {
            var resources = new List<T>();

            using (var operation = _logger.StartTimedOperation(nameof(ListMetaRPResourcesAsync)))
            {
                operation.SetContextProperty(nameof(resourcePath), resourcePath);
                do
                {
                    _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync(_options.UserRPTenantId);
                    _httpClient.DefaultRequestHeaders.Add(MetricTypeHeaderKey, MetricTypeHeaderValue);

                    var response = await _httpClient.GetAsync(listResponse.NextLink);

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
    }
}
