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

        /// <summary>
        /// This is an internal property that is used to optimize queries against the identity.
        /// This property will avoid complex queries on Cosmos DB to find the managed identity document
        /// who has identity that is close to expiry. By persisting the minimum of the renew after
        /// time of all the system and user identities, the query is simplified to just comparing
        /// against this property.
        /// The type of this property is DateTime instead of DateTimeOffset so that the MongoDB driver
        /// serializes it as Date BSON type (https://docs.mongodb.com/manual/reference/bson-types/),
        /// while the driver serializes DateTimeOffset as an array with Ticks and Offset as the two
        /// elements (https://jira.mongodb.org/browse/CSHARP-3181). Using DateTime is preferred because
        /// time range queries against Date BSON type is natively supported by the driver.
        /// </summary>
        [BsonElement("renew_after")]
        public DateTime RenewAfter
        {
            get
            {
                var defaultRenewAfter = DateTimeOffset.UtcNow + TimeSpan.FromDays(1);
                var defaultTicks = defaultRenewAfter.Ticks;

                var systemIdentityRenewAfterTicks = SystemAssignedIdentity?.RenewAfter?.Ticks ?? defaultTicks;
                var minUserIdentitiesRenewAfterTicks = UserAssignedIdentities?.Min(i => i.Value.RenewAfter?.Ticks ?? defaultTicks) ?? defaultTicks;

                return new DateTime(
                    Math.Min(systemIdentityRenewAfterTicks, minUserIdentitiesRenewAfterTicks),
                    DateTimeKind.Utc);
            }
        }
    }
}
