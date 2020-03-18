//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.RPaaS
{
    public class ListResponse<T>
    {
        public IEnumerable<T> Value { get; set; }

        public string NextLink { get; set; }
    }
}
