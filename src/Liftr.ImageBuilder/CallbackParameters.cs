//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Fluent;
using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public class CallbackParameters
    {
        public LiftrAzureFactory LiftrAzureFactory { get; set; }

        public KeyVaultClient KeyVaultClient { get; set; }

        public Serilog.ILogger Logger { get; set; }

        public BuilderOptions BuilderOptions { get; set; }

        public BuilderCommandOptions BuilderCommandOptions { get; set; }

        public IVault KeyVault { get; set; }

        public IStorageAccount VHDStorageAccount { get; set; }

        public IGallery Gallery { get; set; }

        public IGalleryImageVersion ImageVersion { get; set; }

        public Uri VHDUri { get; set; }
    }
}
