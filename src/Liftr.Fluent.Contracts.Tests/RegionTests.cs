//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Liftr.Fluent.Contracts.Tests
{
    public class RegionTests
    {
        [Fact]
        public void RegionShortNameValidation()
        {
            Assert.Equal("wus", Region.USWest.ShortName());

            Assert.Equal("wus2", Region.USWest2.ShortName());

            Assert.Equal("cus", Region.USCentral.ShortName());

            Assert.Equal("eus", Region.USEast.ShortName());

            Assert.Equal("eus2", Region.USEast2.ShortName());

            Assert.Equal("ncus", Region.USNorthCentral.ShortName());

            Assert.Equal("scus", Region.USSouthCentral.ShortName());

            Assert.Equal("wcus", Region.USWestCentral.ShortName());

            Assert.Equal("cca", Region.CanadaCentral.ShortName());

            Assert.Equal("eca", Region.CanadaEast.ShortName());

            Assert.Equal("sbr", Region.BrazilSouth.ShortName());

            Assert.Equal("neu", Region.EuropeNorth.ShortName());

            Assert.Equal("weu", Region.EuropeWest.ShortName());

            Assert.Equal("suk", Region.UKSouth.ShortName());

            Assert.Equal("wuk", Region.UKWest.ShortName());

            Assert.Equal("hk", Region.AsiaEast.ShortName());

            Assert.Equal("sing", Region.AsiaSouthEast.ShortName());

            Assert.Equal("tyo", Region.JapanEast.ShortName());

            Assert.Equal("wjp", Region.JapanWest.ShortName());

            Assert.Equal("eau", Region.AustraliaEast.ShortName());

            Assert.Equal("seau", Region.AustraliaSouthEast.ShortName());

            Assert.Equal("cau", Region.AustraliaCentral.ShortName());

            Assert.Equal("cau2", Region.AustraliaCentral2.ShortName());

            Assert.Equal("cin", Region.IndiaCentral.ShortName());

            Assert.Equal("sin", Region.IndiaSouth.ShortName());

            Assert.Equal("win", Region.IndiaWest.ShortName());

            Assert.Equal("skr", Region.KoreaSouth.ShortName());

            Assert.Equal("sel", Region.KoreaCentral.ShortName());

            Assert.Equal("bj", Region.ChinaNorth.ShortName());

            Assert.Equal("sha", Region.ChinaEast.ShortName());
        }

        [Fact]
        public void RegionToTextAndBack()
        {
            var regionType = typeof(Region);
            var fields = regionType
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.FieldType.Name.OrdinalEquals(nameof(Region)) & !f.Name.Contains("gov", StringComparison.OrdinalIgnoreCase));

            int i = 0;
            foreach (var region in fields)
            {
                VerifyRegionToTextAndBack((Region)region.GetValue(null));
                i++;
            }

            Assert.Equal(46, i);
        }

        [Fact]
        public void InvalidRegionWillThrow()
        {
            var r = Region.Create("InvalidRegionName");
            Assert.Throws<ArgumentOutOfRangeException>(() => { r.ShortName(); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { "InvalidRegionName".ParseShortAzureRegion(); });
        }

        private static void VerifyRegionToTextAndBack(Region location)
        {
            Assert.Equal(location, location.ShortName().ParseShortAzureRegion());
        }
    }
}
