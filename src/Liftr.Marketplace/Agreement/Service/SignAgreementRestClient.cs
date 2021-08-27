//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Marketplace.Agreement.Interfaces;
using Microsoft.Liftr.Marketplace.Agreement.Options;
using Microsoft.Liftr.Marketplace.Exceptions;
using Microsoft.Liftr.Marketplace.Utils;
using Microsoft.Liftr.TokenManager;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Agreement.Service
{
    public class SignAgreementRestClient : ISignAgreementRestClient
    {
        private readonly string _endpoint;
        private readonly string _apiVersion;
        private readonly MarketplaceAgreementOptions _marketplaceAgreementOptions;
        private readonly ILogger _logger;
        private readonly CertificateStore _certStore;
        private readonly Uri _kvEndpoint;
        private readonly string _certName;
        private readonly AuthenticationTokenCallback _authenticationTokenCallback;
        private readonly IHttpClientFactory _httpClientFactory;

        public SignAgreementRestClient(
            MarketplaceAgreementOptions marketplaceAgreementOptions,
            ILogger logger,
            CertificateStore certStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certStore = certStore ?? throw new ArgumentNullException(nameof(certStore));
            _marketplaceAgreementOptions = marketplaceAgreementOptions ?? throw new ArgumentNullException(nameof(marketplaceAgreementOptions));

            _endpoint = marketplaceAgreementOptions.API.Endpoint.ToString();
            _apiVersion = marketplaceAgreementOptions.API.ApiVersion;
            _kvEndpoint = marketplaceAgreementOptions.AuthOptions.KeyVaultEndpoint;
            _certName = marketplaceAgreementOptions.AuthOptions.CertificateName;
        }

        public SignAgreementRestClient(
             Uri endpoint,
             string apiVersion,
             ILogger logger,
             IHttpClientFactory httpClientFactory,
             AuthenticationTokenCallback authenticationTokenCallback)
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
        }

        public SignAgreementRestClient(
            MarketplaceAgreementOptions marketplaceAgreementOptions,
            CertificateStore certStore)
            : this(marketplaceAgreementOptions, LoggerFactory.ConsoleLogger, certStore)
        {
        }

        public SignAgreementRestClient(
            Uri endpoint,
            string apiVersion,
            IHttpClientFactory httpClientFactory,
            AuthenticationTokenCallback authenticationTokenCallback)
            : this(endpoint, apiVersion, LoggerFactory.ConsoleLogger, httpClientFactory, authenticationTokenCallback)
        {
        }

        public delegate Task<X509Certificate2> AuthenticationTokenCallback();

#nullable enable
        public async Task<T> SendRequestAsync<T>(
           HttpMethod method,
           string requestPath,
           Dictionary<string, string>? additionalHeaders = null,
           object? content = null,
           CancellationToken cancellationToken = default) where T : class
        {
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            var cert = await _certStore.GetCertificateAsync(_kvEndpoint, _certName); // Get X509Certificate Object from KeyVault using cert name for every request

            using var httpRequestHandler = HttpRequestHelper.GetHttpHandlerForCertAuthentication(cert);

            using var httpClient = new HttpClient(httpRequestHandler);

            using var request = HttpRequestHelper.CreateRequest(_endpoint, _apiVersion, method, requestPath, requestId, correlationId, additionalHeaders);
            _logger.Information($"Sending request method: {method}, requestUri: {request.RequestUri}, requestId: {requestId}, correlationId: {correlationId} for Agreement API");
            HttpResponseMessage? response = null;

            try
            {
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

                var res = await response.Content.ReadAsStringAsync();
                var result = res.FromJson<T>();
                _logger.Information($"Request: {request.RequestUri} succeeded for Agreement call");

                return result;
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"The request: {method}:{request.RequestUri} failed for Agreement call";
                if (ex.Message != null)
                {
                    errorMessage += $"Reason: {ex.Message}";
                }

                _logger.Error(errorMessage);
                throw;
            }
        }

        public async Task<T> SendRequestUsingTokenServiceAsync<T>(
          HttpMethod method,
          string requestPath,
          Dictionary<string, string>? additionalHeaders = null,
          object? content = null,
          CancellationToken cancellationToken = default) where T : class
        {
            var requestId = Guid.NewGuid(); // Every request should have a different requestId
            var correlationId = TelemetryContext.GetOrGenerateCorrelationId();

            _logger.Information($"[SendRequestUsingTokenServiceAsync] Accessing Cert object for the request {requestPath}");

            var cert = await _authenticationTokenCallback();

            _logger.Information($"[SendRequestUsingTokenServiceAsync] Certificate object is successfully created for the request {requestPath}");

            if (cert == null)
            {
                throw new MarketplaceException($"[SendRequestUsingTokenServiceAsync] Cert Object is found to be null for the request {requestPath}");
            }

            _logger.Information($"For the request {requestPath}. Cert Details: {cert.Subject}, {cert.IssuerName}, {cert.Issuer}");

            using var httpRequestHandler = HttpRequestHelper.GetHttpHandlerForCertAuthentication(cert);

            using var httpClient = new HttpClient(httpRequestHandler);

            using var request = HttpRequestHelper.CreateRequest(_endpoint, _apiVersion, method, requestPath, requestId, correlationId, additionalHeaders);
            _logger.Information($"Sending request method: {method}, requestUri: {request.RequestUri}, requestId: {requestId}, correlationId: {correlationId} for Agreement API");
            HttpResponseMessage? response = null;

            try
            {
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

                var res = await response.Content.ReadAsStringAsync();
                var result = res.FromJson<T>();
                _logger.Information($"Request: {request.RequestUri} succeeded for Agreement call");

                return result;
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"The request: {method}:{request.RequestUri} failed for Agreement call";
                if (ex.Message != null)
                {
                    errorMessage += $"Reason: {ex.Message}";
                }

                _logger.Error(errorMessage);
                throw;
            }
        }
#nullable disable
    }
}
