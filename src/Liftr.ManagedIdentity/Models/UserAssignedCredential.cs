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
        /// The ARM resource ID of the user assigned identity.
        /// </summary>
        [JsonProperty("resource_id")]
        public string ResourceId { get; set; }
    }
}
