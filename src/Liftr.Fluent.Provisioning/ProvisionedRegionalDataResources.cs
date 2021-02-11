//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Contracts;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ProvisionedRegionalDataResources : BaseProvisionedRegionalDataResources
    {
        public IIdentity ManagedIdentity { get; set; }

        public IVault KeyVault { get; set; }

        public IStorageAccount StorageAccount { get; set; }

        public RPAssetOptions RPAssetOptions { get; set; }

        public DataAssetOptions DataAssetOptions { get; set; }
    }
}
