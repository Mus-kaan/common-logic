//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.ManagedIdentity.Models
{
    /// <summary>
    /// The parameters that describe the credential of an user assigned identity.
    /// </summary>
    public class UserAssignedCredential : IdentityCredential
    {
        /// <summary>
        /// The tenant ID under which the user assigned identity is created.
        /// </summary>
        [JsonProperty("tenant_id")]
        public string TenantId { get; set; }

        /// <summary>
        /// The object/principal ID from AAD identifying the service principal backing the identity.
        /// </summary>
        [JsonProperty("object_id")]
        public string ObjectId { get; set; }

        /// <summary>
        /// The ARM resource ID of the user assigned identity.
        /// </summary>
        [JsonProperty("resource_id")]
        public string ResourceId { get; set; }
    }
}
