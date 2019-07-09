//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts.ARM
{
    public class ARMResource : IARMResource
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string Location { get; set; }
    }
}
