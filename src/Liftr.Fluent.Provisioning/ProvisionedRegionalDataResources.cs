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

        /// <summary>
        /// Storage account for Thanos object store. https://github.com/thanos-io/thanos/blob/main/docs/storage.md
        /// </summary>
        public IStorageAccount ThanosStorageAccount { get; set; }

        public DataAssetOptions DataAssetOptions { get; set; }
    }
}
