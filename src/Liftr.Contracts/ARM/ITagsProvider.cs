//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// the tags provider for an Azure resource.
    /// </summary>
    public interface ITagsProvider
    {
        /// <summary>
        /// The tags of the resource.
        /// </summary>
        IDictionary<string, string> Tags { get; set; }
    }
}
