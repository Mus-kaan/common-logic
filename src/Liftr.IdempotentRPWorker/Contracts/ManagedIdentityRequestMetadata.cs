//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class ManagedIdentityRequestMetadata
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
        [JsonProperty(PropertyName = "x-ms-identity-url")]
        public string IdentityUrl { get; set; }

        [JsonProperty(PropertyName = "x-ms-identity-principal-id")]
        public string IdentityPrincipalId { get; set; }

        [JsonProperty(PropertyName = "x-ms-home-tenant-id")]
        public string HomeTenantId { get; set; }
    }
}
