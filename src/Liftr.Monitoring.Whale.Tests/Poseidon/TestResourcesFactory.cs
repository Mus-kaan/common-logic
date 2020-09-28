//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale.Poseidon.Tests
{
    /// <summary>
    /// Factory for management operations on test resources, allowing
    /// creation, deletion and retrieval of definition.
    /// </summary>
    public class TestResourcesFactory
    {
        private readonly IAzureClientsProvider _clientsProvider;

        public TestResourcesFactory()
        {
            _clientsProvider = new PoseidonClientsProvider();
        }

        /// <summary>
        /// Create, if needed, public IP addresses 1 and 2. Public IP 1 will contain the
        /// inclusion tag and public IP 2 will contain both the inclusion and the exclusion tag.
        /// </summary>
        public async Task CreateResoucesAsync()
        {
            var fluentClient = await _clientsProvider.GetFluentClientAsync(TestCredentials.SubscriptionId, TestCredentials.TenantId);
            var publicIps = await fluentClient.PublicIPAddresses
                .ListByResourceGroupAsync(PoseidonConstants.TestResourceGroup);

            var inclusionTag = PoseidonConstants.InclusionFilteringTag;
            var exclusionTag = PoseidonConstants.ExclusionFilteringTag;

            if (!publicIps.Any(p => p.Name.OrdinalEquals(PoseidonConstants.PublicIp1Name)))
            {
                await fluentClient.PublicIPAddresses
                    .Define(PoseidonConstants.PublicIp1Name)
                    .WithRegion(PoseidonConstants.TestLocationName)
                    .WithExistingResourceGroup(PoseidonConstants.TestResourceGroup)
                    .WithSku(PublicIPSkuType.Basic)
                    .WithDynamicIP()
                    .WithTag(inclusionTag.Name, inclusionTag.Value)
                    .CreateAsync();
            }

            if (!publicIps.Any(p => p.Name.OrdinalEquals(PoseidonConstants.PublicIp2Name)))
            {
                await fluentClient.PublicIPAddresses
                    .Define(PoseidonConstants.PublicIp2Name)
                    .WithRegion(PoseidonConstants.TestLocationName)
                    .WithExistingResourceGroup(PoseidonConstants.TestResourceGroup)
                    .WithSku(PublicIPSkuType.Basic)
                    .WithDynamicIP()
                    .WithTag(inclusionTag.Name, inclusionTag.Value)
                    .WithTag(exclusionTag.Name, exclusionTag.Value)
                    .CreateAsync();
            }
        }

        /// <summary>
        /// Retrieve the definition for the currently existing
        /// public IPs on the test resource group.
        /// </summary>
        public async Task<IEnumerable<IPublicIPAddress>> GetResourcesAsync()
        {
            var fluentClient = await _clientsProvider.GetFluentClientAsync(TestCredentials.SubscriptionId, TestCredentials.TenantId);
            var publicIps = await fluentClient.PublicIPAddresses
                .ListByResourceGroupAsync(PoseidonConstants.TestResourceGroup);

            return publicIps;
        }

        /// <summary>
        /// Delete, if needed, the public IP addresses 1 and 2.
        /// </summary>
        public async Task DeleteResourcesAsync()
        {
            var fluentClient = await _clientsProvider.GetFluentClientAsync(TestCredentials.SubscriptionId, TestCredentials.TenantId);
            var publicIps = await fluentClient.PublicIPAddresses
                .ListByResourceGroupAsync(PoseidonConstants.TestResourceGroup);

            if (publicIps.Any(p => p.Name.OrdinalEquals(PoseidonConstants.PublicIp1Name)))
            {
                await fluentClient.PublicIPAddresses
                    .DeleteByResourceGroupAsync(
                        PoseidonConstants.TestResourceGroup, PoseidonConstants.PublicIp1Name);
            }

            if (publicIps.Any(p => p.Name.OrdinalEquals(PoseidonConstants.PublicIp2Name)))
            {
                await fluentClient.PublicIPAddresses
                    .DeleteByResourceGroupAsync(
                        PoseidonConstants.TestResourceGroup, PoseidonConstants.PublicIp2Name);
            }
        }
    }
}
