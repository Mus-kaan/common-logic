﻿//-----------------------------------------------------------------------------
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
        [PublicCentralUS]
        public void PublicCentralUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USCentral.Name, TestAzureRegion.Name);
            Assert.Equal("PublicCentralUS", TestRegionCategory);
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
