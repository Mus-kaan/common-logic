//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class AzureRegionTests
    {
        [Fact]
        public void AzureRegionShortNameValidation()
        {
            Assert.Equal("wus", AzureRegion.USWest.ShortName);

            Assert.Equal("wus2", AzureRegion.USWest2.ShortName);

            Assert.Equal("cus", AzureRegion.USCentral.ShortName);

            Assert.Equal("eus", AzureRegion.USEast.ShortName);

            Assert.Equal("eus2", AzureRegion.USEast2.ShortName);

            Assert.Equal("ncus", AzureRegion.USNorthCentral.ShortName);

            Assert.Equal("scus", AzureRegion.USSouthCentral.ShortName);

            Assert.Equal("wcus", AzureRegion.USWestCentral.ShortName);

            Assert.Equal("cca", AzureRegion.CanadaCentral.ShortName);

            Assert.Equal("eca", AzureRegion.CanadaEast.ShortName);

            Assert.Equal("sbr", AzureRegion.BrazilSouth.ShortName);

            Assert.Equal("neu", AzureRegion.EuropeNorth.ShortName);

            Assert.Equal("weu", AzureRegion.EuropeWest.ShortName);

            Assert.Equal("suk", AzureRegion.UKSouth.ShortName);

            Assert.Equal("wuk", AzureRegion.UKWest.ShortName);

            Assert.Equal("hk", AzureRegion.AsiaEast.ShortName);

            Assert.Equal("sing", AzureRegion.AsiaSouthEast.ShortName);

            Assert.Equal("tyo", AzureRegion.JapanEast.ShortName);

            Assert.Equal("wjp", AzureRegion.JapanWest.ShortName);

            Assert.Equal("eau", AzureRegion.AustraliaEast.ShortName);

            Assert.Equal("seau", AzureRegion.AustraliaSouthEast.ShortName);

            Assert.Equal("cau", AzureRegion.AustraliaCentral.ShortName);

            Assert.Equal("cau2", AzureRegion.AustraliaCentral2.ShortName);

            Assert.Equal("cin", AzureRegion.IndiaCentral.ShortName);

            Assert.Equal("sin", AzureRegion.IndiaSouth.ShortName);

            Assert.Equal("win", AzureRegion.IndiaWest.ShortName);

            Assert.Equal("skr", AzureRegion.KoreaSouth.ShortName);

            Assert.Equal("sel", AzureRegion.KoreaCentral.ShortName);

            Assert.Equal("bj", AzureRegion.ChinaNorth.ShortName);

            Assert.Equal("sha", AzureRegion.ChinaEast.ShortName);
        }

        [Fact]
        public void AzureRegionToTextAndBack()
        {
            var azureRegionType = typeof(AzureRegion);
            var fields = azureRegionType
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.FieldType.Name.OrdinalEquals(nameof(AzureRegion)) & !f.Name.Contains("gov", StringComparison.OrdinalIgnoreCase));

            int i = 0;
            foreach (var region in fields)
            {
                VerifyRegionToTextAndBack((AzureRegion)region.GetValue(null));
                i++;
            }

            Assert.Equal(48, i);
        }

        [Fact]
        public void InvalidRegionWillThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new AzureRegion("InvalidRegionName"); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { AzureRegion.FromShortName("asfa"); });
        }

        [Theory]
        [InlineData("eastasia", "East Asia", "hk")]
        [InlineData("southindia", "South India", "sin")]
        public void CheckNameMapping(string name, string displayName, string shortName)
        {
            {
                var parsedName = new AzureRegion(name);
                Assert.Equal(name, parsedName.Name);
                Assert.Equal(displayName, parsedName.DisplayName);
                Assert.Equal(shortName, parsedName.ShortName);
            }

            {
                var parsedName = new AzureRegion(displayName);
                Assert.Equal(name, parsedName.Name);
                Assert.Equal(displayName, parsedName.DisplayName);
                Assert.Equal(shortName, parsedName.ShortName);
            }

            {
                var parsedName = AzureRegion.FromShortName(shortName);
                Assert.Equal(name, parsedName.Name);
                Assert.Equal(displayName, parsedName.DisplayName);
                Assert.Equal(shortName, parsedName.ShortName);
            }
        }

        private static void VerifyRegionToTextAndBack(AzureRegion location)
        {
            Assert.Equal(location, AzureRegion.FromShortName(location.ShortName));
        }
    }
}
