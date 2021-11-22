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

        /// <summary>
        /// The certificate cannot be used to auth to AAD or MSI data plane before this time
        /// </summary>
        [BsonElement("not_before")]
        [BsonRepresentation(BsonType.Document)]
        public DateTimeOffset? NotBefore { get; set; }

        /// <summary>
        /// The certificate cannot be used to auth to AAD after this time.
        /// </summary>
        [BsonElement("not_after")]
        [BsonRepresentation(BsonType.Document)]
        public DateTimeOffset? NotAfter { get; set; }

        /// <summary>
        /// The certificate can be renewed via the MSI data plane after this time.
        /// </summary>
        [BsonElement("renew_after")]
        [BsonRepresentation(BsonType.Document)]
        public DateTimeOffset? RenewAfter { get; set; }

        /// <summary>
        /// The certificate cannot be used to auth to MSI data plane after this time.
        /// </summary>
        [BsonElement("cannot_renew_after")]
        [BsonRepresentation(BsonType.Document)]
        public DateTimeOffset? CannotRenewAfter { get; set; }
    }
}
