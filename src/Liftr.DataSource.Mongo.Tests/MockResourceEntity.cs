//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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
}
