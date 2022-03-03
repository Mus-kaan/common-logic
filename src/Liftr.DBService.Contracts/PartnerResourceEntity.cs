//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class PartnerResourceEntity : BaseRpEntity
    {
        public PartnerResourceEntity(string liftrResourceId, string partnerResourceId)
        {
            ResourceId = liftrResourceId;
            PartnerResourceId = partnerResourceId;
        }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("partnerResourceId")]
        public string PartnerResourceId { get; set; }

        [EntityUpdateAttribute(allowed: true)]
        [BsonElement("ssoUrl")]
        public Uri SSOUrl { get; set; }
    }
}