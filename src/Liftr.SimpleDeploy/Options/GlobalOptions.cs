//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Newtonsoft.Json;

namespace Microsoft.Liftr.SimpleDeploy
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
