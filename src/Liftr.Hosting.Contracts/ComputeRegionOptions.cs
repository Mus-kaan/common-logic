//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class ComputeRegionOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string ComputeBaseName { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public bool SupportAvailabilityZone { get; set; } = true;

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(ComputeBaseName)} cannot be null or empty.");
            }

            if (SupportAvailabilityZone)
            {
                RegionOptions.ValidateAKSZoneLocation(Location);
            }
        }
    }
}
