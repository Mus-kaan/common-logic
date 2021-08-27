//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

/*
 * This class will interact with the Gateway Token Service to fetch
 * FPA Based JWT Token for Create and Delete calls and
 * X509Certificate2 object for Agreement API calls
 */

using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Utils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.TokenService
{
    public sealed class TokenServiceRestClient : ITokenServiceRestClient, IDisposable
    {
        private readonly string _endpoint;
        private readonly string _apiVersion;
        private readonly ILogger _logger;
        private readonly AuthenticationTokenCallback _authenticationTokenCallback;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(MarketplaceConstants.SemaphoreInitialThreadCount, MarketplaceConstants.SemaphoreMaxThreadCount);
        private readonly Dictionary<string, TokenCacheItem> _cachedToken = new Dictionary<string, TokenCacheItem>();

        public TokenServiceRestClient(
            Uri endpoint,
            string apiVersion,
            ILogger logger,
            IHttpClientFactory httpClientFactory,
            AuthenticationTokenCallback authenticationTokenCallback,
            TimeSpan? cacheTTL = null)
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentException("message", nameof(apiVersion));
            }

            _endpoint = endpoint.ToString();
            _apiVersion = apiVersion;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _authenticationTokenCallback = authenticationTokenCallback ?? throw new ArgumentNullException(nameof(authenticationTokenCallback));
            CacheTTL = cacheTTL ?? TimeSpan.FromMinutes(MarketplaceConstants.DefaultTTL);

            logger.Information($"Run '{nameof(GetTokenAsync)}' to make sure the TokenServiceRestClient is initialized correctly.");
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            var token = GetTokenAsync().Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("[TokenServiceRestClient] Cannot load FPA Token from the Token service");
            }

            logger.Information($"Run '{nameof(GetCertificateAsync)}' to make sure the TokenServiceRestClient is initialized correctly.");
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            using var cert = GetCertificateAsync().Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("[TokenServiceRestClient] Cannot load cert object from the Token service");
            }
        }

        public TokenServiceRestClient(
            Uri endpoint,
            string apiVersion,
            IHttpClientFactory httpClientFactory,
            AuthenticationTokenCallback authenticationTokenCallback)
            : this(endpoint, apiVersion, LoggerFactory.ConsoleLogger, httpClientFactory, authenticationTokenCallback)
        {
        }

        public delegate Task<string> AuthenticationTokenCallback();

        public TimeSpan CacheTTL { get; }

        public void Dispose()
        {
            _tokenSemaphore.Dispose();
        }

        public async Task<string> GetTokenAsync()
        {
            var cancellationToken = default(CancellationToken);
            return await SendRequestAsync(HttpMethod.Get, MarketplaceConstants.TokenRequestPath, cancellationToken: cancellationToken);
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var cancellationToken = default(CancellationToken);
            var secertBundle = await SendRequestAsync(HttpMethod.Get, MarketplaceConstants.CertificateRequestPath, cancellationToken: cancellationToken);
            X509Certificate2 cert = GetX509Certificate2Cert(secertBundle);
            return cert ?? throw new MarketplaceException($"Cert Object is obtained Null from the Token Service!!");
        }

#nullable enable

        /// <summary>
        /// Send requests to the Token Service for fetching Token generated from FPA and Cert secret bundle
        /// </summary>
        /// <returns></returns>
        private async Task<string> SendRequestAsync(
            HttpMethod method,
            string requestPath,
            Dictionary<string, string>? additionalHeaders = null,
            object? content = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(requestPath))
            {
                throw new ArgumentNullException(nameof(requestPath));
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var accessToken = await _authenticationTokenCallback();

            using var request = HttpRequestHelper.CreateRequest(_endpoint, _apiVersion, method, requestPath, requestId, correlationId, additionalHeaders, accessToken);
            _logger.Information($"Sending request method: {method}, requestUri: {request.RequestUri}, requestId: {requestId}, correlationId: {correlationId} to token service");
            HttpResponseMessage? response = null;

            // Adding Semaphore to provide only 1 Thread access at a time as race condition can lead to acquire expired Token from the Cache
            await _tokenSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Checking the token in the cache wrt token expiry time
                if (!string.IsNullOrWhiteSpace(requestPath) && _cachedToken != null && _cachedToken.ContainsKey(requestPath)
                    && DateTimeOffset.UtcNow < _cachedToken[requestPath].ValidTill)
                {
                    _logger.Information($"Fetching the results from Cache for the request path: {requestPath} and Validity: {_cachedToken[requestPath].ValidTill}");
                    return _cachedToken[requestPath].Token ?? throw new MarketplaceException($"[{nameof(TokenServiceRestClient)}] Cache contains the key but value is found to be null for the request {request.RequestUri} and Validity: {_cachedToken[requestPath].ValidTill}");
                }

                if (content != null)
                {
                    var requestBody = JsonConvert.SerializeObject(content);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    response = await httpClient.SendAsync(request, cancellationToken);
                }
                else
                {
                    response = await httpClient.SendAsync(request, cancellationToken);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw await RequestFailedException.CreateAsync(request, response);
                }

                string? result;
                var jsonResponse = await response.Content.ReadAsStringAsync();
                result = jsonResponse.FromJson<string>();

                // Storing the results back to cache
                if (_cachedToken != null)
                {
                    _logger.Information($"Storing the latest result to Cache for the request path: {requestPath} and updated Validity {DateTimeOffset.UtcNow + CacheTTL}");
                    _cachedToken[requestPath] = new TokenCacheItem()
                    {
                        Token = result,
                        ValidTill = DateTimeOffset.UtcNow + CacheTTL,
                    };
                }

                _logger.Information($"Request: {request.RequestUri} succeeded!!");

                return result ?? throw new MarketplaceException($"[{nameof(TokenServiceRestClient)}] Response from token service client is null for the request {request.RequestUri}");
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"The request: {method}:{request.RequestUri} failed!!";
                if (ex.Message != null)
                {
                    errorMessage += $"Reason: {ex.Message}";
                }

                _logger.Error(errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                var errorMessage = $"The request: {method}:{request.RequestUri} failed!!";
                if (ex.Message != null)
                {
                    errorMessage += $"Reason: {ex.Message}";
                }

                _logger.Error(errorMessage);
                throw;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        private X509Certificate2 GetX509Certificate2Cert(string certSecretBundle)
        {
            byte[] certBytes = Convert.FromBase64String(certSecretBundle);
            _logger.Information($"Certificate Json string is successfully converted into byte array for the request");
            string? password = null;
            X509Certificate2 cert = new X509Certificate2(certBytes, password, X509KeyStorageFlags.Exportable);
            _logger.Information($"Certificate object is successfully created for the request. Cert Details: {cert?.Subject}, {cert?.IssuerName}, {cert?.Issuer}");
            return cert ?? throw new MarketplaceException("Cert object created for Agreement API is null!!");
        }
    }

    internal class TokenCacheItem
    {
        public string? Token { get; set; }

        public DateTimeOffset ValidTill { get; set; }
    }
}
