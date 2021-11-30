//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public class ResourceIdentity
    {
        /// <summary>
        /// The type of the identity.
        /// </summary>
        [BsonElement("identity_type")]
        public string IdentityType { get; set; }

        /// <summary>
        /// The identity metadata for system assigned identity for the run if exists.
        /// </summary>
        [BsonElement("system_assigned_identity")]
        public ManagedIdentityMetadata SystemAssignedIdentity { get; set; }

        /// <summary>
        /// The identity metadata for all user assigned identities for the run if exists.
        /// </summary>
        [BsonElement("user_assigned_identities")]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, ManagedIdentityMetadata> UserAssignedIdentities { get; set; }
    }
}
