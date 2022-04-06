//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Platform.Contracts.Models
{
    public class PullRequest
    {
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
        /// Pull request Title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// PR Description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Source code Url
        /// </summary>
        [JsonProperty("sourceCodeStorageUrl")]
        public Uri SourceCodeStorageUrl { get; set; }

        /// <summary>
        /// List of commits for PR push
        /// </summary>
        [JsonProperty("repositoryId")]
        public string RepositoryId { get; set; }

        /// <summary>
        /// Repository URL
        /// </summary>
        [JsonProperty("repositoryUrl")]
        public Uri RepositoryUrl { get; set; }

        /// <summary>
        /// Repository Name
        /// </summary>
        [JsonProperty("repositoryName")]
        public string RepositoryName { get; set; }
    }
}
