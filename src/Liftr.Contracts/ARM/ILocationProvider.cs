//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// The location provider for an Azure resource.
    /// </summary>
    public interface ILocationProvider
    {
        /// <summary>
        /// The location of the resource. This cannot be changed after the resource is created.
        /// </summary>
        string Location { get; set; }
    }
}
