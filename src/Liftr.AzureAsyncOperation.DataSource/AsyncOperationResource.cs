//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Liftr.AzureAsyncOperation
{
    public enum OperationStatus
    {
        Created = 0,
        Running,
        Succeeded,
        Failed,
        Canceled,
    }

    /// <summary>
    /// ARM long running async operation response as described here.
    /// https://github.com/Azure/azure-resource-manager-rpc/blob/783f3f4a108215dd57ed449d0b4406f913480757/v1.0/Addendum.md#operation-resource-format
    /// </summary>
    public class AsyncOperationResource
    {
        /// <summary>
        /// Long running operation id
        /// </summary>
        [JsonProperty("name")]
        public string OperationId { get; set; }

        /// <summary>
        /// Long running operation status
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OperationStatus Status { get; set; }

        /// <summary>
        /// Long running operation errors if any
        /// </summary>
        public Error Error { get; set; }

        /// <summary>
        /// Long running operation start time
        /// </summary>
        public DateTime StartTime { get; set; }
    }

    public class Error
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }
}
