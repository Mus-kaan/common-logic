//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ProvisionedRegionalDataResources : BaseProvisionedRegionalDataResources
    {
        public IIdentity ManagedIdentity { get; set; }

        public IVault KeyVault { get; set; }

        public IStorageAccount StorageAccount { get; set; }

        [Obsolete("Please use DataAssetOptions instead")]
        public RPAssetOptions RPAssetOptions { get; set; }

        public DataAssetOptions DataAssetOptions { get; set; }
    }
}
