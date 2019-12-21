//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.SBI.Mover
{
    public class SBIVersionInfo
    {
        [JsonProperty(PropertyName = "vhds")]
        public Dictionary<string, string> VHDS { get; set; }
    }
}
