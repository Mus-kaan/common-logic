//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    [SwaggerExtension(ExcludeFromSwagger = true)]
    public class AgreementResourceEntity
    {
        public AgreementResourceEntity(string subscriptionId)
        {
            SubscriptionId = subscriptionId ?? throw new ArgumentNullException(nameof(subscriptionId));
        }

        [BsonId]
        [BsonElement("subId")]
        public string SubscriptionId { get; set; }

        [BsonElement("accepted")]
        public DateTime AcceptedUTC { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedUTC { get; set; } = LiftrDateTime.MinValue;

        [BsonElement("lastModified")]
        public DateTime LastModifiedUTC { get; set; } = LiftrDateTime.MinValue;
    }
}
