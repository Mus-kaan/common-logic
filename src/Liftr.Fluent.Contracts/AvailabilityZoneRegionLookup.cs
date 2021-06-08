//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Linq;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public static class AvailabilityZoneRegionLookup
    {
        // https://docs.microsoft.com/en-us/azure/aks/availability-zones#limitations-and-region-availability  #Add or update locations if doc is updated.
        private static readonly Region[] s_aksZoneSupportRegions =
            {
                Region.AustraliaEast,
                Region.BrazilSouth,
                Region.CanadaCentral,
                Region.USCentral,
                Region.USEast,
                Region.USEast2,
                Region.FranceCentral,
                Region.GermanyWestCentral,
                Region.JapanEast,
                Region.EuropeNorth,
                Region.AsiaSouthEast,
                Region.UKSouth,
                Region.GovernmentUSVirginia,
                Region.EuropeWest,
                Region.USWest2,
                Region.USSouthCentral,
            };

        public static bool HasSupportAKS(Region location, bool noThrow = false)
        {
            var supportAz = s_aksZoneSupportRegions.Contains(location);
            if (!supportAz && !noThrow)
            {
                throw new InvalidHostingOptionException($"Availability Zone support is not provided for region '{location}'. Please verify from this doc: https://docs.microsoft.com/en-us/azure/aks/availability-zones#limitations-and-region-availability ");
            }

            return supportAz;
        }
    }
}
