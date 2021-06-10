//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class AvailabilityZoneRegionLookupTests
    {
        [Fact]
        public void CheckAKSZoneSupport()
        {
            Assert.True(AvailabilityZoneRegionLookup.HasSupportAKS(Region.AustraliaEast));

            Assert.False(AvailabilityZoneRegionLookup.HasSupportAKS(Region.AsiaEast, noThrow: true));

            Assert.Throws<InvalidHostingOptionException>(() =>
            {
                AvailabilityZoneRegionLookup.HasSupportAKS(Region.AsiaEast);
            });
        }

        [Fact]
        public void CheckCosmosDBZoneSupport()
        {
            Assert.True(AvailabilityZoneRegionLookup.HasSupportCosmosDB(Region.CanadaCentral));

            Assert.False(AvailabilityZoneRegionLookup.HasSupportCosmosDB(Region.UAENorth));
        }

        [Fact]
        public void CheckACRZoneSupport()
        {
            Assert.True(AvailabilityZoneRegionLookup.HasSupportACR(Region.EuropeNorth));

            Assert.False(AvailabilityZoneRegionLookup.HasSupportACR(Region.USWest));
        }

        [Fact]
        public void CheckStorgeZoneSupport()
        {
            Assert.True(AvailabilityZoneRegionLookup.HasSupportStorage(Region.BrazilSouth));

            Assert.False(AvailabilityZoneRegionLookup.HasSupportStorage(Region.IndiaWest));
        }

        [Fact]
        public void CheckPostgresZoneSupport()
        {
            Assert.True(AvailabilityZoneRegionLookup.HasSupportPostgresSQL(Region.EuropeWest));

            Assert.False(AvailabilityZoneRegionLookup.HasSupportPostgresSQL(Region.FranceSouth));
        }
    }
}
