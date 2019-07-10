//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts
{
    public interface ITimeSource
    {
        DateTime UtcNow { get; }
    }
}
