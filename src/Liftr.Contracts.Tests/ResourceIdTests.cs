//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Liftr.Contracts.Tests
{
    public class ResourceIdTests
    {
        [Fact]
        public void CanParseManagementGroupResourceId()
        {
            string resourceIdString = "/providers/Microsoft.Management/managementGroups/managementGroupName";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.ManagementGroup, rid.RootScopeLevel);
            Assert.False(rid.HasRoutingScope);
            Assert.Equal("managementGroupName", rid.ManagementGroupName);
            Assert.Null(rid.SubscriptionId);
            Assert.Null(rid.ResourceGroup);
            Assert.Null(rid.Provider);
            Assert.Null(rid.ResourceType);
            Assert.Null(rid.ResourceName);
            Assert.Null(rid.TypedNames);
        }

        [Fact]
        public void CanParseManagementGroupResourceId2()
        {
            string resourceIdString = "/providers/Microsoft.Management/managementGroups/managementGroupName/";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.ManagementGroup, rid.RootScopeLevel);
            Assert.False(rid.HasRoutingScope);
            Assert.Equal("managementGroupName", rid.ManagementGroupName);
            Assert.Null(rid.SubscriptionId);
            Assert.Null(rid.ResourceGroup);
            Assert.Null(rid.Provider);
            Assert.Null(rid.ResourceType);
            Assert.Null(rid.ResourceName);
            Assert.Null(rid.TypedNames);
        }

        [Fact]
        public void CanParseManagementGroupResourceId3()
        {
            string resourceIdString = "/providers/Microsoft.Management/managementGroups/managementGroupName/providers/Microsoft.Compute/availabilitySets/AvSet";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.ManagementGroup, rid.RootScopeLevel);
            Assert.Equal("managementGroupName", rid.ManagementGroupName);
            Assert.Null(rid.SubscriptionId);
            Assert.Null(rid.ResourceGroup);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("Microsoft.Compute", rid.Provider);
            Assert.Equal("availabilitySets", rid.ResourceType);
            Assert.Equal("AvSet", rid.ResourceName);

            Assert.Single(rid.TypedNames);
            Assert.Equal("availabilitySets", rid.TypedNames[0].ResourceType);
            Assert.Equal("AvSet", rid.TypedNames[0].ResourceName);
        }

        [Fact]
        public void CanParseExtensionResourceId()
        {
            string resourceIdString = "/SUBSCRIPTIONS/EEBFBFDB-4167-49F6-BE43-466A6709609F/PROVIDERS/MICROSOFT.FEATURES/PROVIDERS/MICROSOFT.COMPUTE/FEATURES/GALLERYPREVIEW";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.Extension, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal("EEBFBFDB-4167-49F6-BE43-466A6709609F", rid.SubscriptionId);
            Assert.Equal("MICROSOFT.COMPUTE", rid.Provider);
            Assert.Equal("FEATURES", rid.ResourceType);
            Assert.Equal("GALLERYPREVIEW", rid.ResourceName);
        }

        [Fact]
        public void CanParseExtensionResourceId2()
        {
            string resourceIdString = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/myresourcegroup1/providers/microsoft.web/sites/mysite1/providers/Microsoft.Authorization/roleAssignments/9b6a0af6-a75f-4d3c-b18b-3da1dff7e6f0";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.Extension, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal("d21a525e-7c86-486d-a79e-a4f3622f639a", rid.SubscriptionId);
            Assert.Equal("Microsoft.Authorization", rid.Provider);
            Assert.Equal("roleAssignments", rid.ResourceType);
            Assert.Equal("9b6a0af6-a75f-4d3c-b18b-3da1dff7e6f0", rid.ResourceName);
        }

        [Fact]
        public void CanParseTenantResoucrId()
        {
            string resourceIdString = "/tenants/7918d4b5-0442-4a97-be2d-36f9f9962ece/providers/Microsoft.aadiam";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.Tenant, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal("7918d4b5-0442-4a97-be2d-36f9f9962ece", rid.TenantId);
            Assert.Equal("Microsoft.aadiam", rid.Provider);
        }

        [Fact]
        public void CanParseEmptyTenantResoucrId()
        {
            string resourceIdString = "/providers/Microsoft.Authorization/roleAssignments/9b6a0af6-a75f-4d3c-b18b-3da1dff7e6f0";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.Tenant, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal("/", rid.RootScope);
            Assert.Equal("Microsoft.Authorization", rid.Provider);
            Assert.Equal("roleAssignments", rid.ResourceType);
            Assert.Equal("9b6a0af6-a75f-4d3c-b18b-3da1dff7e6f0", rid.ResourceName);
        }

        [Fact]
        public void CanParseSubscriptionId()
        {
            string resourceIdString = "/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.Subscription, rid.RootScopeLevel);
            Assert.False(rid.HasRoutingScope);
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

            Assert.Equal(RootScopeLevel.ResourceGroup, rid.RootScopeLevel);
            Assert.False(rid.HasRoutingScope);
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

            Assert.Equal(RootScopeLevel.ResourceGroup, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
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
        public void CanParseSubscriptionLogResourceId()
        {
            string resourceIdString = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/resourcegroups/testing/providers/microsoft.disks/type576_type576_1_osdisk_1_e6ca31687024444bb940722df0f9109d";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.ResourceGroup, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", rid.SubscriptionId);
            Assert.Equal("testing", rid.ResourceGroup);
            Assert.Equal("microsoft.disks", rid.Provider);
        }

        [Fact]
        public void CanParse3PartResourceId()
        {
            string resourceIdString = "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/LiftrComDevImgRG/providers/Microsoft.Compute/galleries/LiftrComDevSIG/images/ComDevSBI/versions/0.6.13241130";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(RootScopeLevel.ResourceGroup, rid.RootScopeLevel);
            Assert.True(rid.HasRoutingScope);
            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("eebfbfdb-4167-49f6-be43-466a6709609f", rid.SubscriptionId);
            Assert.Equal("images", rid.ChildResourceType);
            Assert.Equal("ComDevSBI", rid.ChildResourceName);

            var rid2 = new ResourceId(rid.SubscriptionId, rid.ResourceGroup, rid.Provider, rid.ResourceType, rid.ResourceName, rid.ChildResourceType, rid.ChildResourceName, rid.GrandChildResourceType, rid.GrandChildResourceName);
            Assert.Equal(rid.ToString(), rid2.ToString());
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

            Assert.Null(rid.ChildResourceType);
            Assert.Null(rid.ChildResourceName);
            Assert.Null(rid.GrandChildResourceType);
            Assert.Null(rid.GrandChildResourceName);

            Assert.Single(rid.TypedNames);
            Assert.Equal("availabilitySets", rid.TypedNames[0].ResourceType);
            Assert.Equal("AvSet", rid.TypedNames[0].ResourceName);

            Assert.Throws<FormatException>(() =>
            {
                ResourceId.FromResourceUri("https://portal.azure.com/?feature.customportal=false#@microsoft.onmicrosoft.com/resource/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet");
            });
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
        public void CanParseGrandChildResourceId()
        {
            string resourceIdString = "/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-acr-rg/providers/Microsoft.ContainerRegistry/registries/liftrmsacr/replication/eastus/grandchild/grandchildname";
            var rid = new ResourceId(resourceIdString);

            Assert.Equal(resourceIdString, rid.ToString());
            Assert.Equal("eebfbfdb-4167-49f6-be43-466a6709609f", rid.SubscriptionId);
            Assert.Equal("liftr-acr-rg", rid.ResourceGroup);
            Assert.Equal("Microsoft.ContainerRegistry", rid.Provider);
            Assert.Equal("registries", rid.ResourceType);
            Assert.Equal("liftrmsacr", rid.ResourceName);
            Assert.Equal("replication", rid.ChildResourceType);
            Assert.Equal("eastus", rid.ChildResourceName);
            Assert.Equal("grandchild", rid.GrandChildResourceType);
            Assert.Equal("grandchildname", rid.GrandChildResourceName);

            Assert.Equal(3, rid.TypedNames.Length);
            Assert.Equal("registries", rid.TypedNames[0].ResourceType);
            Assert.Equal("liftrmsacr", rid.TypedNames[0].ResourceName);
            Assert.Equal("replication", rid.TypedNames[1].ResourceType);
            Assert.Equal("eastus", rid.TypedNames[1].ResourceName);
        }

        [Theory]
        [InlineData("asdasasfdsf")]
        [InlineData("/subscriptionss/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/AresourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/Aproviders/Microsoft.Compute/availabilitySets/AvSet")]
        [InlineData("subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/providers/Microsoft.Compute/availabilitySets/AvSet")]
        public void ParseInvalidFormatThrow(string resourceIdString)
        {
            Assert.Throws<FormatException>(() =>
            {
                new ResourceId(resourceIdString);
            });

            Assert.False(ResourceId.TryParse(resourceIdString, out _));
        }

        [Fact]
        public void InvalidWillThrow()
        {
            Assert.Throws<FormatException>(() =>
            {
                new ResourceId("/SUBSCRIPTIONS/EEBFBFDB-4167-49F6-BE43-466A6709609F/PROVIDERS");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                new ResourceId(null);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                new ResourceId(string.Empty);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                new ResourceId("");
            });

            Assert.Throws<FormatException>(() =>
            {
                new ResourceId("/providers/Microsoft.Management/managementGroups/mgname/asdasd");
            });
        }

        [Theory]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service/")]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service")]
        public void TryParseValidResrouceIds(string resourceIdString)
        {
            Assert.True(ResourceId.TryParse(resourceIdString, out var parsedResourceIdString));
        }

        [Theory]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a", "d21a525e-7c86-486d-a79e-a4f3622f639a", null)]
        [InlineData("/subscriptions/d21a525e-7c86-486d-a79e-a4f3622f639a/resourceGroups/private-link-service", "d21a525e-7c86-486d-a79e-a4f3622f639a", "private-link-service")]
        public void CanSupportRootScopeOnlyResourceId(string resourceIdString, string subscriptionId, string resourceGroup)
        {
            var rid = new ResourceId(resourceIdString);

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                Assert.Equal(subscriptionId, rid.SubscriptionId);
            }

            if (!string.IsNullOrEmpty(resourceGroup))
            {
                Assert.Equal(resourceGroup, rid.ResourceGroup);
            }

            Assert.Null(rid.Provider);
        }

        [Theory]
        [InlineData("/SUBSCRIPTIONS/EEBFBFDB-4167-49F6-BE43-466A6709609F/PROVIDERS/MICROSOFT.CONTAINERINSTANCE", "EEBFBFDB-4167-49F6-BE43-466A6709609F", "MICROSOFT.CONTAINERINSTANCE")]
        [InlineData("/SUBSCRIPTIONS/1D701E7E-3150-4D33-9279-D4EA03E9110D/PROVIDERS/MICROSOFT.INSIGHTS/DIAGNOSTICSETTINGS/DATADOG_DS_774989A3", "1D701E7E-3150-4D33-9279-D4EA03E9110D", "MICROSOFT.INSIGHTS")]
        [InlineData("/SUBSCRIPTIONS/154EE7AD-4C78-4B1F-97D5-8C534EC45BD6/PROVIDERS/MICROSOFT.AUTHORIZATION/ROLEASSIGNMENTS/8DEB566B-6E92-4552-A95A-8C6526EB8124", "154EE7AD-4C78-4B1F-97D5-8C534EC45BD6", "")]
        [InlineData("/SUBSCRIPTIONS/EEBFBFDB-4167-49F6-BE43-466A6709609F/PROVIDERS/MICROSOFT.RESOURCES/DEPLOYMENTS/DATADOG1591740804488.DATADOG_V0", "", "")]
        [InlineData("/SUBSCRIPTIONS/1D701E7E-3150-4D33-9279-D4EA03E9110D/PROVIDERS/MICROSOFT.INSIGHTS/DIAGNOSTICSETTINGS/DATADOG_DS_19CD7A54", "", "")]
        [InlineData("/SUBSCRIPTIONS/EEBFBFDB-4167-49F6-BE43-466A6709609F/PROVIDERS/MICROSOFT.FEATURES/PROVIDERS/MICROSOFT.VIRTUALMACHINEIMAGES/FEATURES/VIRTUALMACHINETEMPLATEPREVIEW", "", "")]
        [InlineData("/SUBSCRIPTIONS/EEBFBFDB-4167-49F6-BE43-466A6709609F/PROVIDERS/MICROSOFT.FEATURES/PROVIDERS/MICROSOFT.COMPUTE/FEATURES/GALLERYPREVIEW", "", "")]
        [InlineData("/PROVIDERS/MICROSOFT.FEATURES/PROVIDERS/MICROSOFT.COMPUTE/FEATURES/GALLERYPREVIEW", "", "")]
        public void CanSupportSubscriptionRootScope(string resourceIdString, string subscriptionId, string providerName)
        {
            var rid = new ResourceId(resourceIdString);

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                Assert.Equal(subscriptionId, rid.SubscriptionId);
            }

            if (!string.IsNullOrEmpty(providerName))
            {
                Assert.Equal(providerName, rid.Provider);
            }
        }
    }
}
