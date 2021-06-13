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
    public class LiftrAzureTestBaseTests : LiftrAzureTestBase
    {
        public LiftrAzureTestBaseTests(ITestOutputHelper output)
            : base(output, useMethodName: true)
        {
        }

        [JenkinsOnly]
        [PublicWestUS2]
        public void PublicWestUS2Test()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USWest2.Name, TestAzureRegion.Name);
            Assert.Equal("PublicWestUS2", TestRegionCategory);

            Assert.Equal(AzureRegion.USWest2.Name, TestResourceGroup.Region.Name);
        }

        [JenkinsOnly]
        [DogfoodEastUS]
        public void DogfoodEastUSTest()
        {
            Assert.Equal(CloudType.DogFood, TestCloudType);
            Assert.Equal(AzureRegion.USEast.Name, TestAzureRegion.Name);
            Assert.Equal("DogfoodEastUS", TestRegionCategory);

            Assert.Equal(AzureRegion.USEast.Name, TestResourceGroup.Region.Name);
        }
    }
}
