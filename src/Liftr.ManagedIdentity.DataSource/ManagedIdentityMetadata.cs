//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    [BsonIgnoreExtraElements]
    public class ManagedIdentityMetadata
    {
        /// <summary>
        /// The AAD client id for the managed identity.
        /// </summary>
        [BsonElement("client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// A refreshed version of the URL used to retrieve credentials for the managed identity.
        /// </summary>
        [BsonElement("client_secret_url")]
        public Uri ClientSecretUrl { get; set; }

        /// <summary>
        /// The AAD tenant ID for the managed identity.
        /// </summary>
        [BsonElement("tenant_id")]
        public string TenantId { get; set; }

        /// <summary>
        /// The AAD object ID for the managed identity.
        /// </summary>
        [BsonElement("object_id")]
        public string ObjectId { get; set; }

        /// <summary>
        /// The AAD authentication endpoint for the managed identity. You can make token request toward this authentication endpoint.
        /// </summary>
        [BsonElement("authentication_endpoint")]
        public Uri AuthenticationEndpoint { get; set; }

        /// <summary>
        /// The ARM resource ID of the user assigned identity. The value is NULL for system assigned identity.
        /// </summary>
        [BsonElement("resource_id")]
        public string ResourceId { get; set; }
    }
}
