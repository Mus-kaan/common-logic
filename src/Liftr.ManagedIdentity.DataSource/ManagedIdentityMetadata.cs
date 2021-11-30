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
        /// The Application/client ID from AAD identifying the service principal backing the identity
        /// </summary>
        [BsonElement("client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// A URL which can be used to retrieve the MSI identity. This is same as the x-ms-identity-url header value returned by ARM when an identity is enabled
        /// on a resource.
        /// </summary>
        [BsonElement("client_secret_url")]
        public Uri ClientSecretUrl { get; set; }

        /// <summary>
        /// The tenant ID under which the user assigned identity is created.
        /// </summary>
        [BsonElement("tenant_id")]
        public string TenantId { get; set; }

        /// <summary>
        /// The object/principal ID from AAD identifying the service principal backing the identity.
        /// </summary>
        [BsonElement("object_id")]
        public string ObjectId { get; set; }

        /// <summary>
        /// The ARM resource ID of the user assigned identity. The value is NULL for system assigned identity.
        /// </summary>
        [BsonElement("resource_id")]
        public string ResourceId { get; set; }
    }
}
