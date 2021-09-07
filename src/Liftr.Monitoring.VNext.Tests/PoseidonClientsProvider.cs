//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.TokenManager;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Datadog.Whale.Poseidon
{
    /// <summary>
    /// Provider for Azure clients using test credentials.
    /// </summary>
    public class PoseidonClientsProvider : IAzureClientsProvider
    {
        private readonly ITokenManager _tokenManager;

        public PoseidonClientsProvider()
        {
            var tokenManagerConfiguration = ConfigurationLoader.GetTokenManagerConfiguration();
            _tokenManager = new TokenManager.TokenManager(tokenManagerConfiguration);
        }

        /// <summary>
        /// Provider for the IAzure fluent client using test credentials.
        /// </summary>
        public async Task<IAzure> GetFluentClientAsync(string subscriptionId, string tenantId)
        {
            var credentials = TestCredentials.GetAzureCredentials();
            var fluentClient = Azure.Management.Fluent.Azure
                .Authenticate(credentials)
                .WithSubscription(TestCredentials.SubscriptionId);

            await Task.Yield();
            return fluentClient;
        }

        /// <summary>
        /// Provider for the IResourceGraphClient using test credentials.
        /// </summary>
        public async Task<IResourceGraphClient> GetResourceGraphClientAsync(string tenantId)
        {
            var token = await _tokenManager.GetTokenAsync(
                TestCredentials.ClientId, TestCredentials.ClientSecret);

            var armEndpoint = new Uri("https://management.azure.com/");
            var resourceGraphClient = new ResourceGraphClient(armEndpoint, new TokenCredentials(token));

            return resourceGraphClient;
        }
    }
}
