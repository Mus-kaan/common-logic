//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class ResourceIdTests
    {
        [Fact]
        public void CanParseResourceId()
        {
            string resourceIdString = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("d21a525e-7c86-486d-a79e-a4f3622f639a", rid.SubscriptionId);
            Assert.Equal("private-link-service", rid.ResourceGroup);
            Assert.Equal("Microsoft.Compute", rid.Provider);
            Assert.Equal("availabilitySets", rid.ResourceType);
            Assert.Equal("AvSet", rid.ResourceName);

            Assert.Single(rid.TypedNames);
            Assert.Equal("availabilitySets", rid.TypedNames[0].ResourceType);
            Assert.Equal("AvSet", rid.TypedNames[0].ResourceName);
        }

        [Fact]
        public void CanParseResourceUri()
        {
            string resourceUri = "https://portal.azure.com/?feature.customportal=false#@microsoft.onmicrosoft.com/resource/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet";
            var rid = ResourceId.FromResourceUri(resourceUri);

            Assert.Equal("d21a525e-7c86-486d-a79e-a4f3622f639a", rid.SubscriptionId);
            Assert.Equal("private-link-service", rid.ResourceGroup);
            Assert.Equal("Microsoft.Compute", rid.Provider);
            Assert.Equal("availabilitySets", rid.ResourceType);
            Assert.Equal("AvSet", rid.ResourceName);

            Assert.Single(rid.TypedNames);
            Assert.Equal("availabilitySets", rid.TypedNames[0].ResourceType);
            Assert.Equal("AvSet", rid.TypedNames[0].ResourceName);
        }

        [Theory]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/")]
        [InlineData("asdasasfdsf")]
        [InlineData("/subscriptionss/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/AresourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/Aproviders/Microsoft.Compute/availabilitySets/AvSet")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet/")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet/sdf")]
        [InlineData("subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet")]
        public void ParseInvalidFormatThrow(string resourceIdString)
        {
            Assert.Throws<FormatException>(() =>
            {
                new ResourceId(resourceIdString);
            });
        }
    }
}
