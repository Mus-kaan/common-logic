//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public interface IManagedIdentityResourceEntity : IResourceEntity
    {
        List<ManagedIdentityMetadata> ManagedIdentities { get; set; }
    }
}
