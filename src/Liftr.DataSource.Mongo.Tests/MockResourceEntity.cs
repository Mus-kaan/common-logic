//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public class MockResourceEntity : BaseResourceEntity
    {
        /// <summary>
        /// VNet Id
        /// </summary>
        [BsonElement("vnet")]
        public string VNet { get; set; }
    }

    public class ExtendedMockResourceEntity : MockResourceEntity
    {
        [BsonElement("provisionState")]
        [BsonRepresentation(BsonType.String)]
        public Microsoft.Liftr.Contracts.ProvisioningState ProvisioningState { get; set; } = Microsoft.Liftr.Contracts.ProvisioningState.Succeeded;
    }
}
