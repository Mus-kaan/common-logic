//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class RegionalComputeOptions
    {
        public string GlobalKeyVaultResourceId { get; set; }

        public string LogAnalyticsWorkspaceResourceId { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public void CheckValues()
        {
            if (string.IsNullOrEmpty(GlobalKeyVaultResourceId))
            {
                throw new InvalidHostingOptionException($"{nameof(GlobalKeyVaultResourceId)} is not valid.");
            }

            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(DataBaseName)} is not valid.");
            }

            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(ComputeBaseName)} is not valid.");
            }
        }
    }
}
