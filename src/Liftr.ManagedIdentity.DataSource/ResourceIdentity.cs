//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.Collections.Generic;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public class ResourceIdentity
    {
        /// <summary>
        /// The type of the managed identity.
        /// </summary>
        [BsonElement("identity_type")]
        public string IdentityType { get; set; }

        /// <summary>
        /// The metadata of the system assigned identity for the resource.
        /// </summary>
        [BsonElement("system_assigned_identity")]
        public ManagedIdentityMetadata SystemAssignedIdentity { get; set; }

        /// <summary>
        /// The metadata of the user assigned identities for the resource.
        /// </summary>
        [BsonElement("user_assigned_identities")]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, ManagedIdentityMetadata> UserAssignedIdentities { get; set; }
    }
}
