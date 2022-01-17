//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class PartnerResourceEntity : BaseEntity
    {
        public PartnerResourceEntity(string liftrResourceId, string partnerId)
        {
            ResourceId = liftrResourceId;
            Id = partnerId;
        }

        [BsonElement("ssoUrl")]
        public Uri SSOUrl { get; set; }
    }
}