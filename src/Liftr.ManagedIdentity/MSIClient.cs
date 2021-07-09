//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Liftr.ManagedIdentity.Models;
using Microsoft.Liftr.TokenManager;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Liftr.ManagedIdentity.MSIClientException;
using HttpBearerChallenge = Microsoft.Liftr.Utilities.HttpBearerChallenge;

namespace Microsoft.Liftr.ManagedIdentity
{
    public class MSIClient : IMSIClient
    {
        private const string MSI_ApiVersion = "2018-10-01-PREVIEW";
        private const string JsonMediaType = "application/json";
        private const string BearerAuthenticationScheme = "Bearer";

        private readonly MSIClientOptions _options;
        private readonly CertificateStore _certStore;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private bool _disposed = false;

        public MSIClient(
            MSIClientOptions options,
            ILogger logger,
            IHttpClientFactory httpClientFactory,
            IKeyVaultClient kvClient)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.CheckValues();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certStore = new CertificateStore(kvClient, _logger);
            _httpClient = httpClientFactory != null
                ? httpClientFactory.CreateClient(nameof(MSIClient))
                : throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <inheritdoc/>
        public async Task<IdentityMetadata> GetIdentityMetadataAsync(
            string identityUrl,
            IEnumerable<string> userAssignedIdentityResourceIds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(identityUrl))
            {
                throw new ArgumentNullException(nameof(identityUrl));
            }

            var rids = string.Join(",", userAssignedIdentityResourceIds);

            using var operation = _logger.StartTimedOperation(nameof(GetIdentityMetadataAsync));
            operation.SetContextProperty(nameof(identityUrl), identityUrl);
            operation.SetContextProperty(nameof(userAssignedIdentityResourceIds), rids);

            try
            {
                var identityMetadata = await GetIdentityMetadataInternalAsync(
                    identityUrl: identityUrl,
                    userAssignedIdentityResourceIds: userAssignedIdentityResourceIds,
                    cancellationToken: cancellationToken);

                operation.SetResultDescription("Successfully got identity metadata.");
                return identityMetadata;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Failed to get identity metadata from {identityUrl} for user-assigned managed identities {userAssignedIdentityResourceIds}。",
                    identityUrl,
                    rids);

                operation.FailOperation(ex.Message);

                if (ex is MSIClientException)
                {
                    throw;
                }

                throw IdentityServiceException(
                        message: "Unexpected error in fetching identity metadata",
                        innerException: ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _httpClient?.Dispose();
                _certStore?.Dispose();
            }

            _disposed = true;
        }

        private async Task<IdentityMetadata> GetIdentityMetadataInternalAsync(
            string identityUrl,
            IEnumerable<string> userAssignedIdentityResourceIds,
            CancellationToken cancellationToken)
        {
            // Identity URL is a well defined URL provided by ARM and is safe to just append api version
            // rather than using URI builder.
            var identityUrlWithApiVersion = $"{identityUrl}&api-version={MSI_ApiVersion}";

            // Step 1: Make an challenge request to the identity URL with the api version.
            // Inspect the  WWW-Authenticate headers to get the authority and the resource against which we can
            // fetch the AD token to make the subsequent credentials call.
            // Making an unauthenticated call to MSI is the recommended approach to get the AuthorityUrl, Tenant and Resource information:
            // https://armwiki.azurewebsites.net/authorization/managed_identities/MSIOnboardingInteractionWithMSI.html#interacting-with-managed-service-identity---msi
            var authBearerChallenge = await GetHttpBearerChallengeAsync(identityUrlWithApiVersion);

            if (authBearerChallenge.AuthorizationAuthority == null ||
                string.IsNullOrEmpty(authBearerChallenge.Resource))
            {
                throw IdentityServiceException(
                        message: "Authority or Resource values are missing in the challenge header.");
            }

            // Get the access token to talk to MSI service using first party certificate.
            var firstPartyAuthResult = await GetFirstPartyAuthTokenAsync(
                authBearerChallenge.AuthorizationAuthority,
                authBearerChallenge.Resource);

            if (firstPartyAuthResult == null || firstPartyAuthResult.AccessToken == null)
            {
                throw IdentityServiceException(
                        message: "Could not fetch the access token to POST to identity service.");
            }

            // Step 2: Using the first party access token, make a POST request to
            // the MSI service with payload that includes all the user assigned identity resource Ids.
            var requestPayload = new CredentialRequest
            {
                IdentityIds = userAssignedIdentityResourceIds,
            };

            using (var credentialsRequest = new HttpRequestMessage(HttpMethod.Post, identityUrlWithApiVersion))
            {
                // Add the first party auth header.
                credentialsRequest.Headers.Authorization = new AuthenticationHeaderValue(
                    BearerAuthenticationScheme,
                    firstPartyAuthResult.AccessToken);

                // add the request content.
                credentialsRequest.Content = new StringContent(
                    content: JsonConvert.SerializeObject(requestPayload),
                    encoding: Encoding.UTF8,
                    mediaType: JsonMediaType);

                var credentialsResponse = await _httpClient.SendAsync(credentialsRequest, cancellationToken);

                string responseContent = null;

                if (credentialsResponse.Content != null)
                {
                    responseContent = await credentialsResponse
                        .Content
                        .ReadAsStringAsync();
                }

                if (credentialsResponse.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(responseContent))
                    {
                        throw IdentityServiceException(
                            message: "Credential request succeeded but the response content is missing.");
                    }

                    return JsonConvert.DeserializeObject<IdentityMetadata>(responseContent);
                }

                switch (credentialsResponse.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.Unauthorized:
                        throw IdentityClientException(
                            message: $"Credential request failed with client error '{credentialsResponse.StatusCode}'. Reason: '{responseContent}'");
                    default: break;
                }

                throw IdentityServiceException(
                    message: $"Credential request failed with status code '{credentialsResponse.StatusCode}'. Reason: '{responseContent}'");
            }
        }

        private async Task<HttpBearerChallenge> GetHttpBearerChallengeAsync(
            string identityUrlWithApiVersion)
        {
            using (var challengeRequest = new HttpRequestMessage(HttpMethod.Get, identityUrlWithApiVersion))
            {
                var challengeResponse = await _httpClient.SendAsync(challengeRequest);

                // Expected response 401 with WWW-Authenticate headers
                if (challengeResponse.StatusCode != HttpStatusCode.Unauthorized)
                {
                    throw IdentityServiceException(
                        message: $"Challenge request failed. Expected 401 status but got '{challengeResponse.StatusCode}'");
                }

                if (challengeResponse.Headers.WwwAuthenticate == null ||
                    challengeResponse.Headers.WwwAuthenticate.Count != 1)
                {
                    throw IdentityServiceException(
                        message: $"Invalid WWW-Authenticate headers in the challenge request response. The headers are '{challengeResponse.Headers.WwwAuthenticate?.ToString()}'");
                }

                var authenticateHeader = challengeResponse
                    .Headers
                    .WwwAuthenticate
                    .Single()
                    .ToString();

                if (!HttpBearerChallenge.TryParse(authenticateHeader, out HttpBearerChallenge httpBearerChallenge))
                {
                    throw IdentityServiceException(
                        message: $"Did not find Bearer challenge in the  WWW-Authenticate headers in the challenge request response. The header is '{authenticateHeader}'");
                }

                return httpBearerChallenge;
            }
        }

        private async Task<AuthenticationResult> GetFirstPartyAuthTokenAsync(
            Uri authority,
            string resource)
        {
            // Todo: implement a token cache or levearge in-memory cache provided by MSAL
            var defaultScope = $"{resource}/.default";
            var scopes = new List<string> { defaultScope };
            var cert = await _certStore.GetCertificateAsync(_options.KeyVaultEndpoint, _options.CertificateName);

            var app = ConfidentialClientApplicationBuilder
                .Create(_options.ApplicationId)
                .WithCertificate(cert)
                .WithAuthority(authority, false)
                .Build();

            return await app
                .AcquireTokenForClient(scopes)
                .WithSendX5C(true) // Tells MSAL to send the x5c header
                .ExecuteAsync();
        }
    }
}
