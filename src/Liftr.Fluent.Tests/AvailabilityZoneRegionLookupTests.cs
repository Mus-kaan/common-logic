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
    }
}
