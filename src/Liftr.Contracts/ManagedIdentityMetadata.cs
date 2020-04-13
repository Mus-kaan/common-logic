//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.Contracts
{
    [BsonIgnoreExtraElements]
    public class ManagedIdentityMetadata
    {
        /// <summary>
        /// The identity URL got from ARM. This is used to query additional detailed metadata from MI dataplane
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
        public string IdentityUrl { get; set; }

        /// <summary>
        /// Type of the MI
        /// </summary>
        public ManagedIdentityTypes Type { get; set; }

        /// <summary>
        /// AppID (client id) of the MI
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Tenant Id
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Cached MI secret
        /// TODO: not used during MVP. Post MVP we may do persistent cache on the secret, with encrption
        /// For now, this is just for manual test purpose
        /// </summary>
        public string CachedSecret { get; set; }

        /// <summary>
        /// The expiration date of the cached secret
        /// </summary>
        public DateTime SecretExpireDateUTC { get; set; }
    }
}
