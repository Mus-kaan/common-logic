//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public interface IManagedIdentityResourceEntity<T> : IResourceEntity where T : IManagedIdentityMetadata
    {
        List<T> ManagedIdentities { get; set; }
    }
}
