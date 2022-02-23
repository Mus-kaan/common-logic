//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.DBService.Contracts
{
    public class BaseRpEntity : BaseEntity
    {
        [BsonElement("resourceName")]
        public string ResourceName { get; set; }

        [BsonElement("azSubsId")]
        public string AzSubsId { get; set; }

        [BsonElement("tenantId")]
        public string TenantId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [BsonElement("workflowType")]
        public WorkflowTypeEnum WorkflowType { get; set; } // either through create flow or linking;
    }
}