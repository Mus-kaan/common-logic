//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ImageBuilder
{
    public class EnvironmentOptions
    {
        public string TenantId { get; set; }

        public string ProvisioningRunnerClientId { get; set; }

        public string AzureVMImageBuilderObjectId { get; set; }

        public string BaseSBIVerion { get; set; }

        public ArtifactStoreOptions ArtifactOptions { get; set; }

        public SBIMoverOptions SBIMoverOptions { get; set; }
    }
}
