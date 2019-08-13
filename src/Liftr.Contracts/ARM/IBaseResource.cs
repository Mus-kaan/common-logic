//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// The base identifiers for an Azure resource.
    /// </summary>
    public interface IBaseResource
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
        string Type { get; set; }
    }
}
