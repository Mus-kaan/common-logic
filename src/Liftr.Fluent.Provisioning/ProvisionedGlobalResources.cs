//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ProvisionedGlobalResources
    {
        public IResourceGroup ResourceGroup { get; set; }

        public IVault KeyVault { get; set; }

        public IRegistry ContainerRegistry { get; set; }

        public IDnsZone DnsZone { get; set; }

        public ITrafficManagerProfile GlobalTrafficManager { get; set; }

        public ICosmosDBAccount GlobalCosmosDBAccount { get; set; }
    }
}
