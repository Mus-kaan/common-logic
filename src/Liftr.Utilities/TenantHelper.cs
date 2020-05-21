//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Utilities
{
    public sealed class TenantHelper : ITenantHelper, IDisposable
    {
        // TODO: make sure this API version is for all the cloud
        private const string c_getSubscriptionAPIVersion = "2020-01-01";

        private readonly Uri _armEndpoint;
        private readonly HttpClient _httpClient;

        public TenantHelper(Uri armEndpoint)
        {
            _armEndpoint = armEndpoint;
            _httpClient = new HttpClient();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<string> GetTenantIdForSubscriptionAsync(string subscriptionId)
        {
            // https://docs.microsoft.com/en-us/rest/api/resources/subscriptions/get
            var getSubscriptionUrl = new Uri(_armEndpoint, $"subscriptions/{subscriptionId}?api-version={c_getSubscriptionAPIVersion}");

            var response = await _httpClient.GetAsync(getSubscriptionUrl);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("ARM GET subscription call is not returning a HttpBearerChallenge.");
            }

            var authenticationHeaderValue = response.Headers?.WwwAuthenticate?.Single()?.ToString();

            if (HttpBearerChallenge.TryParse(authenticationHeaderValue, out var httpBearerChallenge))
            {
                return httpBearerChallenge.TenantId;
            }
            else
            {
                throw new InvalidOperationException("ARM GET subscription call does not return a challenge in valid format: " + authenticationHeaderValue);
            }
        }
    }
}
