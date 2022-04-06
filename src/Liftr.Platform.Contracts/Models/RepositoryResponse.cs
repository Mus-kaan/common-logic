//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Platform.Contracts.Models
{
    public class RepositoryResponse
    {
        /// <summary>
        /// Repository Id
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Repository URI
        /// </summary>
        public Uri RepositoryUri { get; set; }

        /// <summary>
        /// List of build definition URIs
        /// </summary>
        public List<string> BuildDefinitionUris { get; set; }

        /// <summary>
        /// List of pull requests
        /// </summary>
        public List<string> PullRequests { get; set; }
    }
}
