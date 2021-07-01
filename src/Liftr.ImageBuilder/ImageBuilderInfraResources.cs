//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageBuilderInfraResources
    {
        public IVault KeyVault { get; set; }

        public IGallery ImageGallery { get; set; }

        public IContentStore ArtifactStore { get; set; }

        public IStorageAccount ExportStorageAccount { get; set; }

        public IRegistry ACR { get; set; }
    }
}
