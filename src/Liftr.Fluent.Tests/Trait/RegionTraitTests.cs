//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Tests;
using Microsoft.Liftr.Tests.Utilities.Trait;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class RegionTraitTests : LiftrTestBase
    {
        public RegionTraitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        [DogfoodEastUS]
        public void DogfoodEastUSTest()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.USEast.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodEastUS", TestRegionCategory);
        }

        [Fact]
        [DogfoodEastUS2]
        public void DogfoodEastUS2Test()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.USEast2.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodEastUS2", TestRegionCategory);
        }

        [Fact]
        [DogfoodWestUS]
        public void DogfoodWestUSTest()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.USWest.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodWestUS", TestRegionCategory);
        }

        [Fact]
        [DogfoodWestUS2]
        public void DogfoodWestUS2Test()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.USWest2.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodWestUS2", TestRegionCategory);
        }

        [Fact]
        [DogfoodSouthCentralUS]
        public void DogfoodSouthCentralUSTest()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.USSouthCentral.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodSouthCentralUS", TestRegionCategory);
        }

        [Fact]
        [DogfoodNorthEurope]
        public void DogfoodNorthEuropeTest()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.EuropeNorth.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodNorthEurope", TestRegionCategory);
        }

        [Fact]
        [DogfoodWestEurope]
        public void DogfoodWestEuropeTest()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.EuropeWest.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodWestEurope", TestRegionCategory);
        }

        [Fact]
        [PublicCentralUS]
        public void PublicCentralUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USCentral.Name, TestAzureRegion.Name);
            Assert.Equal("PublicCentralUS", TestRegionCategory);
        }

        [Fact]
        [PublicCentralUSEUAP]
        public void PublicCentralUSEUAPTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.CentralUSEUAP.Name, TestAzureRegion.Name);
            Assert.Equal("PublicCentralUSEUAP", TestRegionCategory);
        }

        [Fact]
        [PublicEastUS2EUAP]
        public void PublicEastUS2EUAPTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.EastUS2EUAP.Name, TestAzureRegion.Name);
            Assert.Equal("PublicEastUS2EUAP", TestRegionCategory);
        }

        [Fact]
        [PublicEastUS2]
        public void PublicEastUS2Test()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USEast2.Name, TestAzureRegion.Name);
            Assert.Equal("PublicEastUS2", TestRegionCategory);
        }

        [Fact]
        [PublicEastUS]
        public void PublicEastUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USEast.Name, TestAzureRegion.Name);
            Assert.Equal("PublicEastUS", TestRegionCategory);
        }

        [Fact]
        [PublicWestCentralUS]
        public void PublicWestCentralUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USWestCentral.Name, TestAzureRegion.Name);
            Assert.Equal("PublicWestCentralUS", TestRegionCategory);
        }

        [Fact]
        [PublicSouthCentralUS]
        public void PublicSouthCentralUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USSouthCentral.Name, TestAzureRegion.Name);
            Assert.Equal("PublicSouthCentralUS", TestRegionCategory);
        }

        [Fact]
        [PublicWestUS2]
        public void PublicWestUS2Test()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USWest2.Name, TestAzureRegion.Name);
            Assert.Equal("PublicWestUS2", TestRegionCategory);
        }

        [Fact]
        [PublicWestUS]
        public void PublicWestUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USWest.Name, TestAzureRegion.Name);
            Assert.Equal("PublicWestUS", TestRegionCategory);
        }
    }
}
