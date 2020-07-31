//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ProvisionedRegionalDataResources
    {
        public IResourceGroup ResourceGroup { get; set; }

        public INetwork VNet { get; set; }

        public IDnsZone DnsZone { get; set; }

        public ITrafficManagerProfile TrafficManager { get; set; }

        public IIdentity ManagedIdentity { get; set; }

        public IVault KeyVault { get; set; }

        public IStorageAccount StorageAccount { get; set; }

        public ICosmosDBAccount CosmosDBAccount { get; set; }

        public RPAssetOptions RPAssetOptions { get; set; }
    }
}
