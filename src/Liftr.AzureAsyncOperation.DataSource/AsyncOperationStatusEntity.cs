//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.AzureAsyncOperation
{
    public class AsyncOperationStatusEntity
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("resource")]
        public AsyncOperationResource Resource { get; set; }

        [BsonElement("timeout")]
        public TimeSpan Timeout { get; set; }

        [BsonElement("state")]
        public string State { get; set; }
    }
}
