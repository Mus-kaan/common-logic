﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class ResourceIdTests
    {
        [Fact]
        public void CanParseSubscriptionId()
        {
            string resourceIdString = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a";
            var rid = new ResourceId(resourceIdString);

            Assert.True(rid.IsSubsriptionId);
            Assert.Equal("d21a525e-7c86-486d-a79e-a4f3622f639a", rid.SubscriptionId);
            Assert.Null(rid.ResourceGroup);
            Assert.Null(rid.Provider);
            Assert.Null(rid.ResourceType);
            Assert.Null(rid.ResourceName);
            Assert.Null(rid.TypedNames);
        }

        [Fact]
        public void CanParseResourceGroupId()
        {
            string resourceIdString = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service";
            var rid = new ResourceId(resourceIdString);

            Assert.True(rid.IsResourceGroupId);
            Assert.Equal("d21a525e-7c86-486d-a79e-a4f3622f639a", rid.SubscriptionId);
            Assert.Equal("private-link-service", rid.ResourceGroup);
            Assert.Null(rid.Provider);
            Assert.Null(rid.ResourceType);
            Assert.Null(rid.ResourceName);
            Assert.Null(rid.TypedNames);
        }

        [Fact]
        public void CanParseResourceId()
        {
            string resourceIdString = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet";
            var rid = new ResourceId(resourceIdString);

            Assert.True(rid.IsFullResourceId);
            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("d21a525e-7c86-486d-a79e-a4f3622f639a", rid.SubscriptionId);
            Assert.Equal("private-link-service", rid.ResourceGroup);
            Assert.Equal("Microsoft.Compute", rid.Provider);
            Assert.Equal("availabilitySets", rid.ResourceType);
            Assert.Equal("AvSet", rid.ResourceName);

            Assert.Single(rid.TypedNames);
            Assert.Equal("availabilitySets", rid.TypedNames[0].ResourceType);
            Assert.Equal("AvSet", rid.TypedNames[0].ResourceName);

            var rid2 = new ResourceId(rid.SubscriptionId, rid.ResourceGroup, rid.Provider, rid.ResourceType, rid.ResourceName);
            Assert.Equal(rid.ToString(), rid2.ToString());
            Assert.Equal(rid.ToString(), new ResourceId(rid2.ToString()).ToString());
        }

        [Fact]
        public void CanParse3PartResourceId()
        {
            string resourceIdString = "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/LiftrComDevImgRG/providers/Microsoft.Compute/galleries/LiftrComDevSIG/images/ComDevSBI/versions/0.6.13241130";
            var rid = new ResourceId(resourceIdString);

            Assert.True(rid.IsFullResourceId);
            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("eebfbfdb-4167-49f6-be43-466a6709609f", rid.SubscriptionId);
            Assert.Equal("images", rid.ChildResourceType);
            Assert.Equal("ComDevSBI", rid.ChildResourceName);
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

        [Fact]
        public void CanParseChildResourceId()
        {
            string resourceIdString = "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftrmsacr/replication/eastus";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("eebfbfdb-4167-49f6-be43-466a6709609f", rid.SubscriptionId);
            Assert.Equal("liftr-acr-rg", rid.ResourceGroup);
            Assert.Equal("Microsoft.ContainerRegistry", rid.Provider);
            Assert.Equal("registries", rid.ResourceType);
            Assert.Equal("liftrmsacr", rid.ResourceName);
            Assert.Equal("replication", rid.ChildResourceType);
            Assert.Equal("eastus", rid.ChildResourceName);

            Assert.Equal(2, rid.TypedNames.Length);
            Assert.Equal("registries", rid.TypedNames[0].ResourceType);
            Assert.Equal("liftrmsacr", rid.TypedNames[0].ResourceName);
            Assert.Equal("replication", rid.TypedNames[1].ResourceType);
            Assert.Equal("eastus", rid.TypedNames[1].ResourceName);

            var rid2 = new ResourceId(rid.SubscriptionId, rid.ResourceGroup, rid.Provider, rid.ResourceType, rid.ResourceName, rid.ChildResourceType, rid.ChildResourceName);
            Assert.Equal(rid2.ToString(), rid.ToString());
        }

        [Fact]
        public void CanParseResourceIdWithoutResourceName()
        {
            string resourceIdString = "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("eebfbfdb-4167-49f6-be43-466a6709609f", rid.SubscriptionId);
            Assert.Equal("liftr-acr-rg", rid.ResourceGroup);
            Assert.Equal("Microsoft.ContainerRegistry", rid.Provider);
        }

        [Theory]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/")]
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

        [Theory]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute")]
        public void TryParseValidResrouceIds(string resourceIdString)
        {
            Assert.True(ResourceId.TryParse(resourceIdString, out var parsedResourceIdString));
        }
    }
}
