//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.ManagedIdentity.Models
{
    /// <summary>
    /// The properties of an identity credential as returned by MSI resource provider.
    /// </summary>
    public class IdentityCredential
    {
        /// <summary>
        /// The AAD client ID for the managed identity.
        /// </summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// The base64 encoded private key X509 certificate for the managed identity.
        /// </summary>
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        /// <summary>
        /// A refreshed version of the URL used to retrieve credentials for the managed identity.
        /// </summary>
        [JsonProperty("client_secret_url")]
        public string ClientSecretUrl { get; set; }

        /// <summary>
        /// The AAD tenant ID for the managed identity.
        /// </summary>
        [JsonProperty("tenant_id")]
        public string TenantId { get; set; }

        /// <summary>
        /// The AAD object ID for the managed identity.
        /// </summary>
        [JsonProperty("object_id")]
        public string ObjectId { get; set; }

        /// <summary>
        /// The AAD authentication endpoint for the managed identity. You can make token request toward this authentication endpoint.
        /// </summary>
        [JsonProperty("authentication_endpoint")]
        public string AuthenticationEndpoint { get; set; }

        /// <summary>
        /// The time at which the managed identity credential becomes valid for retireving AAD tokens.
        /// </summary>
        [JsonProperty("not_before")]
        public DateTimeOffset? NotBefore { get; set; }

        /// <summary>
        /// The time at which the managed identity credential becomes invalid for retireving AAD tokens.
        /// </summary>
        [JsonProperty("not_after")]
        public DateTimeOffset? NotAfter { get; set; }

        /// <summary>
        /// The time after which a call to the managed identity client_secret_url will return a new credential.
        /// </summary>
        [JsonProperty("renew_after")]
        public DateTimeOffset? RenewAfter { get; set; }

        /// <summary>
        /// The time after which the managed identity client_secret cannot be used to call client_secret_url for a refreshed credential.
        /// </summary>
        [JsonProperty("cannot_renew_after")]
        public DateTimeOffset? CannotRenewAfter { get; set; }
    }
}
