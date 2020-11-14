//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using System.Collections.Generic;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class RegionalComputeOptions
    {
        public string GlobalKeyVaultResourceId { get; set; }

        public string LogAnalyticsWorkspaceResourceId { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public string ActiveDBKeyName { get; set; }

        public string SecretPrefix { get; set; }

        public string GlobalStorageResourceId { get; set; }

        public string GlobalCosmosDBResourceId { get; set; }

        public string DomainName { get; set; }

        public bool ZoneRedundant { get; set; }

        public bool EnableThanos { get; set; }

        public Dictionary<string, string> OneCertCertificates { get; set; } = new Dictionary<string, string>();

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
