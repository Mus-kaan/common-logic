//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class PartnerResourceEntity : BaseResourceEntity, IPartnerResourceEntity
    {
        public PartnerResourceEntity()
        {
        }

        public PartnerResourceEntity(IPartnerResourceEntity item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ResourceId = item.ResourceId;
            Active = item.Active;
            ProvisioningState = item.ProvisioningState;
            CreatedUTC = item.CreatedUTC;
            LastModifiedUTC = item.LastModifiedUTC;
            ETag = item.ETag;
            TenantId = item.TenantId;

            ResourceType = item.ResourceType;
            IngestEndpoint = item.IngestEndpoint;
            EncryptedContent = item.EncryptedContent;
            EncryptionKeyResourceId = item.EncryptionKeyResourceId;
            EncryptionAlgorithm = item.EncryptionAlgorithm;
            ContentEncryptionIV = item.ContentEncryptionIV;
        }

        [BsonElement("type")]
        public string ResourceType { get; set; }

        [BsonElement("endpoint")]
        public string IngestEndpoint { get; set; }

        [BsonElement("encrypted")]
        public string EncryptedContent { get; set; }

        [BsonElement("enKey")]
        public string EncryptionKeyResourceId { get; set; }

        [BsonElement("enAlg")]
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; }

        [BsonElement("enIV")]
        [BsonRepresentation(BsonType.Binary)]
        public byte[] ContentEncryptionIV { get; set; }
    }
}
