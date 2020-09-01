//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class RegionOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        /// <summary>
        /// A list of compute regions.
        /// </summary>
        public IEnumerable<ComputeRegionOptions> ComputeRegions { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public bool IsSeparatedDataAndComputeRegion => string.IsNullOrEmpty(ComputeBaseName);

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(DataBaseName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                if (ComputeRegions == null || !ComputeRegions.Any())
                {
                    throw new InvalidHostingOptionException("Cannot find information about the compute location.");
                }

                foreach (var computeRegion in ComputeRegions)
                {
                    computeRegion.CheckValid();
                }
            }
            else
            {
                if (ComputeRegions?.Any() == true)
                {
                    throw new InvalidHostingOptionException("Duplicated compute location information.");
                }
            }
        }
    }
}
