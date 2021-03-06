//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// An Azure resource.
    /// </summary>
    public interface IARMResource : IBaseResource, ILocationProvider, ITagsProvider
    {
    }
}
