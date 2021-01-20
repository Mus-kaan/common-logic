//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ProvisionedComputeResources
    {
        public IIdentity ManagedIdentity { get; set; }

        public IVault KeyVault { get; set; }

        public IStorageAccount StorageAccount { get; set; }

        public IKubernetesCluster AKS { get; set; }

        public ISubnet AKSSubnet { get; set; }

        public IStorageAccount ThanosStorageAccount { get; set; }

        public IVault GlobalKeyVault { get; set; }

        public ICosmosDBAccount GlobalCosmosDB { get; set; }

        public string AKSObjectId { get; set; }

        public string KubeletObjectId { get; set; }

        public RPAssetOptions RPAssetOptions { get; set; }
    }
}
