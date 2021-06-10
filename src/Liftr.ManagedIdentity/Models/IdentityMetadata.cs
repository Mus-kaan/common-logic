//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.ManagedIdentity.Models
{
    /// <summary>
    /// The parameters that describe the metadata for both system and user assigned identities.
    /// </summary>
    public class IdentityMetadata : IdentityCredential
    {
        /// <summary>
        /// An array of user identity properties.
        /// </summary>
        [JsonProperty("explicit_identities")]
        public IEnumerable<UserAssignedCredential> UserAssignedCredentials { get; set; }
    }
}
