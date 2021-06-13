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
        [PublicWestUS2]
        public void PublicWestUS2Test()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USWest2.Name, TestAzureRegion.Name);
        }

        [Fact]
        [PublicEastUS]
        public void PublicEastUSTest()
        {
            Assert.Equal(CloudType.Public, TestCloudType);
            Assert.Equal(AzureRegion.USEast.Name, TestAzureRegion.Name);
        }
    }
}
