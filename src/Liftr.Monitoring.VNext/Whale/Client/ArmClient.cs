//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model;
using Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Options;
using Microsoft.Liftr.TokenManager;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Client
{
    /// <summary>
    /// Use this Arm Client to interact with and to perform CRUD operations on the resources
    /// </summary>
    public class ArmClient : IArmClient
    {
        private const int MaxConcurrentUpdateRequests = 1;
        private const int DelayBetweenUpdateRequestsInSeconds = 1;
        private readonly AzureClientsProviderOptions _azureClientsProviderOptions;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private readonly ITokenManager _tokenManager;
        private readonly ILogger _logger;

        public ArmClient(
            AzureClientsProviderOptions azureClientsProviderOptions,
            CertificateStore certificateStore,
            HttpClient httpClient,
            ILogger logger)
        {
            _azureClientsProviderOptions = azureClientsProviderOptions ?? throw new ArgumentNullException(nameof(azureClientsProviderOptions));
            if(certificateStore == null)
            {
                throw new ArgumentNullException(nameof(certificateStore));
            }
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(MaxConcurrentUpdateRequests);
            _tokenManager = new TokenManager.TokenManager(azureClientsProviderOptions.TokenManagerConfiguration, certificateStore);
        }

        public async Task<string> GetResourceAsync(string resourceId, string apiVersion, string tenantId)
        {
            var uriBuilder = new UriBuilder(Constants.ArmManagementEndpoint)
            {
                Path = resourceId,
                Query = $"api-version={apiVersion}"
            };
            _logger.Information($"Start getting resource at Uri: {uriBuilder.Uri}");

            using var request = await CreateRequestAsync(HttpMethod.Get, uriBuilder.Uri.ToString(), tenantId);
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                var errMsg = $"Failed at getting resource with Id '{resourceId}'. statusCode: '{response.StatusCode}'";
                if (response?.Content != null)
                {
                    errMsg = errMsg + $", response: {await response.Content?.ReadAsStringAsync()}";
                }

                var ex = new InvalidOperationException(errMsg);
                _logger.Error(errMsg);
                throw ex;
            }
        }

        public async Task PutResourceAsync(string resourceId, string apiVersion, string resourceBody, string tenantId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync();

            try
            {
                await SendPutResourceRequestAsync(resourceId, apiVersion, resourceBody, tenantId);
                _logger.Information("Waiting for {DelayBetweenUpdateRequestsInSeconds} before sending the allowing the next requests.", DelayBetweenUpdateRequestsInSeconds);
                await Task.Delay(TimeSpan.FromSeconds(DelayBetweenUpdateRequestsInSeconds));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteResourceAsync(string resourceId, string apiVersion, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            var uriBuilder = new UriBuilder(Constants.ArmManagementEndpoint);
            uriBuilder.Path = resourceId;
            uriBuilder.Query = $"api-version={apiVersion}";
            _logger.Information($"Start deleting resource at Uri: {uriBuilder.Uri}");

            using var request = await CreateRequestAsync(HttpMethod.Delete, uriBuilder.Uri.ToString(), tenantId);
            var deleteResponse = await _httpClient.SendAsync(request);

            _logger.Information($"DELETE response code: {deleteResponse.StatusCode}");

            if (deleteResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Deleting resource at Uri: '{uriBuilder.Uri}' failed with error code '{deleteResponse.StatusCode}'");
                if (deleteResponse?.Content != null)
                {
                    var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                    _logger.Error("Error response body: {errorContent}", errorContent);
                }

                throw new InvalidOperationException($"Delete resource with id '{resourceId}' failed.");
            }

            _logger.Information($"Finished deleting resource at Uri: {uriBuilder.Uri}");
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod httpMethod, string url, string tenantId, StringContent content = null)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException($"'{nameof(tenantId)}' cannot be null or empty.", nameof(tenantId));
            }

            var accessToken = await _tokenManager.GetTokenAsync(_azureClientsProviderOptions.KeyVaultEndpoint, 
                _azureClientsProviderOptions.ClientId,
                _azureClientsProviderOptions.CertificateName,
                tenantId, 
                sendX5c: true);
            
            if (accessToken == null)
            {
                throw new InvalidOperationException($"Unable to obtain token for FPA {_azureClientsProviderOptions.ClientId}");
            }
            
            var request = new HttpRequestMessage(httpMethod, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            if (content != null)
            {
                request.Content = content;
            }

            return request;
        }

        private async Task SendPutResourceRequestAsync(string resourceId, string apiVersion, string resourceBody, string tenantId, CancellationToken cancellationToken = default)
        {
            var uriBuilder = new UriBuilder(Constants.ArmManagementEndpoint);
            uriBuilder.Path = resourceId;
            uriBuilder.Query = $"api-version={apiVersion}";
            _logger.Information($"Start getting resource at Uri: {uriBuilder.Uri}");

            using var httpContent = new StringContent(resourceBody, Encoding.UTF8, "application/json");
            using var request = await CreateRequestAsync(HttpMethod.Put, uriBuilder.Uri.ToString(), tenantId, httpContent);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errMsg = $"Failed at putting resource with Id '{resourceId}'. statusCode: '{response.StatusCode}'";
                if (response?.Content != null)
                {
                    errMsg = errMsg + $", response: {await response.Content?.ReadAsStringAsync()}";
                }

                var ex = new InvalidOperationException(errMsg);
                _logger.Error(errMsg);
                throw ex;
            }

            _logger.Information($"Finished putting resource at Uri: {uriBuilder.Uri}");
        }
    }
}