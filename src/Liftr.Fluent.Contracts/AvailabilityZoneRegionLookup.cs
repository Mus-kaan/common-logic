//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Linq;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public static class AvailabilityZoneRegionLookup
    {
        // Add or update locations if doc is updated.

        // https://docs.microsoft.com/en-us/azure/aks/availability-zones#limitations-and-region-availability
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
                Region.USWest3,
            };

        // https://docs.microsoft.com/en-us/azure/cosmos-db/high-availability#availability-zone-support
        private static readonly Region[] s_cosmosDBSupportRegions =
            {
                Region.BrazilSouth,
                Region.CanadaCentral,
                Region.USCentral,
                Region.USEast,
                Region.USEast2,
                Region.USSouthCentral,
                Region.USWest2,
                Region.USWest3,
                Region.FranceCentral,
                Region.GermanyWestCentral,
                Region.EuropeNorth,
                Region.NorwayEast,
                Region.UKSouth,
                Region.EuropeWest,
                Region.SouthAfricaNorth,
                Region.AustraliaEast,
                Region.IndiaCentral,
                Region.JapanEast,
                Region.KoreaCentral,
                Region.AsiaSouthEast,
                Region.AsiaEast,
            };

        // https://docs.microsoft.com/en-us/azure/postgresql/flexible-server/overview#azure-regions
        private static readonly Region[] s_postgresSQLSupportRegions =
            {
                Region.EuropeWest,
                Region.EuropeNorth,
                Region.UKSouth,
                Region.USEast2,
                Region.USWest2,
                Region.USCentral,
                Region.USEast,
                Region.AsiaSouthEast,
                Region.JapanEast,
                Region.AustraliaEast,
                Region.CanadaCentral,
                Region.FranceCentral,
                Region.GermanyWestCentral,
                Region.USSouthCentral,
            };

        // https://docs.microsoft.com/en-us/azure/storage/common/storage-redundancy#zone-redundant-storage
        private static readonly Region[] s_storageSupportedRegions =
            {
                Region.SouthAfricaNorth,
                Region.AsiaSouthEast,
                Region.AustraliaEast,
                Region.JapanEast,
                Region.CanadaCentral,
                Region.EuropeNorth,
                Region.EuropeWest,
                Region.FranceCentral,
                Region.GermanyWestCentral,
                Region.UKSouth,
                Region.BrazilSouth,
                Region.USCentral,
                Region.USEast,
                Region.USEast2,
                Region.USSouthCentral,
                Region.USWest2,
            };

        // https://docs.microsoft.com/en-us/azure/container-registry/zone-redundancy#preview-limitations
        private static readonly Region[] s_acrSupportedRegions =
            {
                Region.USEast,
                Region.USEast2,
                Region.USWest2,
                Region.EuropeNorth,
                Region.EuropeWest,
                Region.JapanEast,
            };

        public static bool HasSupportAKS(Region location, bool noThrow = false)
        {
            return CheckSupport(location, s_aksZoneSupportRegions, "https://docs.microsoft.com/en-us/azure/aks/availability-zones#limitations-and-region-availability", noThrow);
        }

        public static bool HasSupportCosmosDB(Region location)
        {
            return CheckSupport(location, s_cosmosDBSupportRegions, "https://docs.microsoft.com/en-us/azure/cosmos-db/high-availability#availability-zone-support", true);
        }

        public static bool HasSupportPostgresSQL(Region location)
        {
            return CheckSupport(location, s_postgresSQLSupportRegions, "https://docs.microsoft.com/en-us/azure/postgresql/flexible-server/overview#azure-regions", true);
        }

        public static bool HasSupportStorage(Region location)
        {
            return CheckSupport(location, s_storageSupportedRegions, "https://docs.microsoft.com/en-us/azure/storage/common/storage-redundancy#zone-redundant-storage", true);
        }

        public static bool HasSupportACR(Region location)
        {
            return CheckSupport(location, s_acrSupportedRegions, "https://docs.microsoft.com/en-us/azure/container-registry/zone-redundancy#preview-limitations", true);
        }

        private static bool CheckSupport(Region location, Region[] supportedRegions, string doc, bool noThrow = false)
        {
            var supportAz = supportedRegions.Contains(location);
            if (!supportAz && !noThrow)
            {
                throw new InvalidHostingOptionException($"Availability Zone support is not provided for region '{location}'. Please verify from this doc: {doc} ");
            }

            return supportAz;
        }
    }
}
