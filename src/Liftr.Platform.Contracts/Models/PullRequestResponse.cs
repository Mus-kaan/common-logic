//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Platform.Contracts.Models
{
    /// <summary>
    /// Create PR response
    /// </summary>
    ///
    public class PullRequestResponse
    {
        /// <summary>
        /// PR id
        /// </summary>
        [JsonProperty("pullRequestId")]
        public string PullRequestId { get; set; }

        /// <summary>
        /// PR Status
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// PR creation date
        /// </summary>
        [JsonProperty("creationDate")]
        public string CreationDate { get; set; }

        /// <summary>
        /// Source reference branch
        /// </summary>
        [JsonProperty("sourceRefName")]
        public string SourceRefName { get; set; }

        /// <summary>
        /// Target reference branch
        /// </summary>
        [JsonProperty("targetRefName")]
        public string TargetRefName { get; set; }

        /// <summary>
        /// Pull Request Created By
        /// </summary>
        [JsonProperty("createdBy")]
        public PullRequestCreatedBy CreatedBy { get; set; }

        /// <summary>
        /// Pull Request Url
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}
