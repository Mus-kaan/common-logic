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
        /// Kubenetes version. This one will overwrite the global setting of <seealso cref="AKSInfo.KubernetesVersion"/>.
        /// </summary>
        public string KubernetesVersion { get; set; }

        /// <summary>
        /// A list of compute regions.
        /// </summary>
        public IEnumerable<ComputeRegionOptions> ComputeRegions { get; set; }

        public bool ZoneRedundant { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public bool IsSeparatedDataAndComputeRegion => string.IsNullOrEmpty(ComputeBaseName);

        public bool SupportAvailabilityZone { get; set; } = true;

        public void CheckValid(bool isAKS)
        {
            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(DataBaseName)} cannot be null or empty.");
            }

            if (IsSeparatedDataAndComputeRegion)
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

                if (isAKS && SupportAvailabilityZone)
                {
                    ValidateAKSZoneLocation(Location);
                }
            }
        }

        public static void ValidateAKSZoneLocation(Region location)
        {
            // https://docs.microsoft.com/en-us/azure/aks/availability-zones#limitations-and-region-availability  #Add or update locations if doc is updated.
            Region[] locationsWithAvailabilityZone =
                {
                Region.AustraliaEast,
                Region.CanadaCentral,
                Region.USCentral,
                Region.USEast,
                Region.USEast2,
                Region.FranceCentral,
                Region.JapanEast,
                Region.EuropeNorth,
                Region.AsiaSouthEast,
                Region.UKSouth,
                Region.EuropeWest,
                Region.USWest2,
                };

            if (locationsWithAvailabilityZone.Contains(location))
            {
                return;
            }

            throw new InvalidHostingOptionException($"Availability Zone support is not provided for region '{location}'. Please verify from this doc: https://docs.microsoft.com/en-us/azure/aks/availability-zones#limitations-and-region-availability ");
        }
    }
}
