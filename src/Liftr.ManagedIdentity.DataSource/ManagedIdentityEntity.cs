//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public class ManagedIdentityEntity : BaseResourceEntity
    {
        [BsonElement("identity")]
        public ResourceIdentity Identity { get; set; }
    }
}
