//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    [BsonIgnoreExtraElements]
    public class ManagedIdentityMetadata : IManagedIdentityMetadata
    {
        [BsonElement("identityUrl")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
        public string IdentityUrl { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public ManagedIdentityTypes Type { get; set; }

        [BsonElement("appId")]
        public string AppId { get; set; }

        [BsonElement("tenantId")]
        public string TenantId { get; set; }

        [BsonElement("cachedSecret")]
        public string CachedSecret { get; set; }

        [BsonElement("secretExpireDateUTC")]
        public DateTime SecretExpireDateUTC { get; set; }
    }
}
