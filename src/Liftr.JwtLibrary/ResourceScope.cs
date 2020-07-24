//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.JwtLibrary
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenRequestTypes
    {
        Default,
        ManagedIdentityToken,
        LiftrBillingToken,
        LiftrWebhookToken,
    }

    public class ResourceScope
    {
        public const string ReadAction = "read";
        public const string WriteAction = "write";
        public const string DeleteAction = "delete";
        public static readonly string[] FullActions = new string[] { ReadAction, WriteAction, DeleteAction };

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "type")]
        public TokenRequestTypes Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "actions")]
        public string[] Actions { get; set; }
    }
}
