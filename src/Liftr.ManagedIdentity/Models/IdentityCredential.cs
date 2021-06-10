//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.ManagedIdentity.Models
{
    /// <summary>
    /// The properties of an identity credentials as returned by MSI resource provider.
    /// </summary>
    public class IdentityCredential
    {
        /// <summary>
        /// The Application/client ID from AAD identifying the service principal backing the identity
        /// </summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// A base64 encoded string containing certificate bytes
        /// </summary>
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        /// <summary>
        /// A URL which can be used to retrieve the MSI identity. This is same as the x-ms-identity-url header value returned by ARM when an identity is enabled
        /// on a resource.
        /// </summary>
        [JsonProperty("client_secret_url")]
        public string ClientSecretUrl { get; set; }

        /// <summary>
        /// The certificate cannot be used to auth to AAD or MSI data plane before this time
        /// </summary>
        [JsonProperty("not_before")]
        public DateTimeOffset? NotBefore { get; set; }

        /// <summary>
        /// The certificate cannot be used to auth to AAD after this time.
        /// </summary>
        [JsonProperty("not_after")]
        public DateTimeOffset? NotAfter { get; set; }

        /// <summary>
        /// The certificate can be renewed via the MSI data plane after this time.
        /// </summary>
        [JsonProperty("renew_after")]
        public DateTimeOffset? RenewAfter { get; set; }

        /// <summary>
        /// The certificate cannot be used to auth to MSI data plane after this time.
        /// </summary>
        [JsonProperty("cannot_renew_after")]
        public DateTimeOffset? CannotRenewAfter { get; set; }
    }
}
