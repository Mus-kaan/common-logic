//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class GlobalOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string BaseName { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(BaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(BaseName)} cannot be null or empty.");
            }
        }
    }
}
