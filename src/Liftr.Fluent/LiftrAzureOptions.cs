//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Polly;
using System.Net.Http;

namespace Microsoft.Liftr.Fluent
{
    public class LiftrAzureOptions
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/key-vault/key-vault-ovw-storage-keys#service-principal-application-id
        /// </summary>
        public string AzureKeyVaultObjectId { get; set; } = "6a72ed84-c8e9-45e2-adcf-ebe22993ed17"; // ame

        public IAsyncPolicy<HttpResponseMessage> HttpPolicy { get; set; } = HttpPolicyHelper.GetDefaultPolicy();
    }
}
