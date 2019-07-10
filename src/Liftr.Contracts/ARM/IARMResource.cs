//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// An Azure resource.
    /// </summary>
    public interface IARMResource
    {
        /// <summary>
        /// The resource ID.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// The tags of the resource.
        /// </summary>
        IDictionary<string, string> Tags { get; set; }

        /// <summary>
        /// The location of the resource. This cannot be changed after the resource is created.
        /// </summary>
        string Location { get; set; }
    }
}
