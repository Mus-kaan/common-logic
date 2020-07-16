//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Fluent
{
    public class LiftrAzureOptions
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/key-vault/key-vault-ovw-storage-keys#service-principal-application-id
        /// </summary>
        public string AzureKeyVaultObjectId { get; set; } = "6a72ed84-c8e9-45e2-adcf-ebe22993ed17"; // ame

        /// <summary>
        /// Kubenetes version. This need to be updated every few months.
        /// az aks get-versions
        /// https://aka.ms/supported-version-list
        /// https://github.com/kubernetes/kubernetes/blob/master/CHANGELOG/README.md
        /// </summary>
        public string KubernetesVersion { get; set; } = "1.17.7";
    }
}
