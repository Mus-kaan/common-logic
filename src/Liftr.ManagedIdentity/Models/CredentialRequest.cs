//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.ManagedIdentity.Models
{
    /// <summary>
    /// The request parameters for fetching the metadata for user assigned identities.
    /// </summary>
    public class CredentialRequest
    {
        [JsonProperty(PropertyName = "identityIds")]
        public IEnumerable<string> IdentityIds { get; set; }
    }
}
