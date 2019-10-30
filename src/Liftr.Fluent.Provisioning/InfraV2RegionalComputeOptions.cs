//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InfraV2RegionalComputeOptions
    {
        public string CentralKeyVaultResourceId { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public string SecretPrefix { get; set; }

        public string CopyKVSecretsWithPrefix { get; set; }

        public string ProvisioningSPNClientId { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public void CheckValues()
        {
            if (string.IsNullOrEmpty(CentralKeyVaultResourceId))
            {
                throw new InvalidOperationException($"{nameof(CentralKeyVaultResourceId)} is not valid.");
            }

            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidOperationException($"{nameof(DataBaseName)} is not valid.");
            }

            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                throw new InvalidOperationException($"{nameof(ComputeBaseName)} is not valid.");
            }

            if (string.IsNullOrEmpty(SecretPrefix))
            {
                throw new InvalidOperationException($"{nameof(SecretPrefix)} is not valid.");
            }

            if (string.IsNullOrEmpty(CopyKVSecretsWithPrefix))
            {
                throw new InvalidOperationException($"{nameof(CopyKVSecretsWithPrefix)} is not valid.");
            }

            if (string.IsNullOrEmpty(ProvisioningSPNClientId))
            {
                throw new InvalidOperationException($"{nameof(ProvisioningSPNClientId)} is not valid.");
            }
        }
    }
}
