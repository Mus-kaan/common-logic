//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.TrafficManager.Fluent;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class BaseProvisionedRegionalDataResources
    {
        public IResourceGroup ResourceGroup { get; set; }

        public INetwork VNet { get; set; }

        public IDnsZone DnsZone { get; set; }

        public ITrafficManagerProfile TrafficManager { get; set; }

        public ICosmosDBAccount CosmosDBAccount { get; set; }
    }
}
