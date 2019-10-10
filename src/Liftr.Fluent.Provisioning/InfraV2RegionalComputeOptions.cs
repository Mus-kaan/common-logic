//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InfraV2RegionalComputeOptions
    {
        public string CosmosDBResourceId { get; set; }

        public string CentralKeyVaultResourceId { get; set; }

        public string KVDBSecretName { get; set; }

        public string CopyKVSecretsWithPrefix { get; set; }

        public string ProvisioningSPNClientId { get; set; }

        public void CheckValues()
        {
            if (string.IsNullOrEmpty(CosmosDBResourceId))
            {
                throw new InvalidOperationException($"{nameof(CosmosDBResourceId)} is not valid.");
            }

            if (string.IsNullOrEmpty(CentralKeyVaultResourceId))
            {
                throw new InvalidOperationException($"{nameof(CentralKeyVaultResourceId)} is not valid.");
            }

            if (string.IsNullOrEmpty(KVDBSecretName))
            {
                throw new InvalidOperationException($"{nameof(KVDBSecretName)} is not valid.");
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
