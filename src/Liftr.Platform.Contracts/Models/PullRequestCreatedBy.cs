//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Platform.Contracts.Models
{
    public class PullRequestCreatedBy
    {
        /// <summary>
        /// Creator's id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
