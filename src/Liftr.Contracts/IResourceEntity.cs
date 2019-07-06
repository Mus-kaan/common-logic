//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Liftr.Contracts
{
    public interface IResourceEntity : IEntityId
    {
        string ETag { get; set; }
    }
}
