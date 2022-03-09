//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class IPPoolManagerTests
    {
        private static readonly string s_sampleSSHPublic = "c3NoLXJzYSBBQUFBQjNOemFDMXljMkVBQUFBREFRQUJBQUFDQVFEZDI0RzgxOXZBRStxeE9nQmdmYmhsTVdHeTZCdHRYZHc3Q2tnRzE0THFMbmFBYld4SUdLNFlXcVJ1MHFCSXBndlhaVWFaek9tUmVHWDNuRjQ4SndnM0RZVldrbGIvSGYxRk51RUtNME1KZUtwdnd5c1ozRkUvamdRa1ZkQWxxQXBteXgxVnJxRW9pVGd0b3h2dktDbmNHOUtsRWhqRHdNbXdmZW01Z3Y5MVVkcjhZSkdHdTBvNjYrN3RZY1prZVphTWUxVCtlUW5ZVWF5Q3ZFTGo2R0QyVTBhckp2a25VemxNYTVkajVGdDdkNGZTbzRCYUtNY0RldFhSb3R5M2pGWFBBMkZubmNRNG93YW9HU1VBUU5SalRCeURtUUpUYlhqUWs5bUY5RUZiZmw0QWVsbXFZSDdrMjA2QUQxZHQ0bkYwWTFnZHg0d2lZRFlFaGRRSWl3Vi85WEsxL1lGR1hieGhWQ1k0aTVsZjBhVkNsUUl5WXBxb1c3RVAzRlN3L1ZKRUZWa1A0VDN2M1JPaFBrL1o2MTVYdU54LzhVK2tQbEhac09QTVJyR1dWZ3N2UENYMzRvL3EwdHBKaDNBbHBIUnV3dWt4aUsrdWRmTTFtVjRTV041Q0tYalQ2TDEzUzVZNjJjRVJoVHZpMHZnQldGQ0dCUC9LWWJiVDV5RkJmWWhYR291VUxsYXY3VGRYb29pT3Uvc3hCcnoxQ2duM1lqUWVNWGlHU2dkaVZIMEZSWHYrVjJEaHNaRWh1L1dhang0alc3Ui9qYVZqTUExMXc5d2Z0cWNPK1JFbktyaHhWY0IrZ1lkSTlESy9qNmlNWmsrN1NvQk9ySVhDK1ByRmJpSjQ5SVU1Y3FscXU5Y1RUMnhXQ2xRWE5sb1oybkR3Z1BYSTBQck5NSWZmc1E9PSByb290QHdrLWNhYXMtOTJlMzNmNmU5NDRhNDRhM2IyYWI0ZjU4MWQ5MDNiZDgtYWY5Njg1ZThiOGYzZTdmYmUwYTZjMw==".FromBase64();

        private readonly ITestOutputHelper _output;

        public IPPoolManagerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyAKSIPPoolAsync()
        {
            var shortPartnerName = "ip-pool";
            var location = TestCommon.Location;
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, location);
            TestCommon.AddCommonTags(context.Tags);

            var baseName = SdkContext.RandomResourceName("v", 3);
            var prefix = context.GenerateCommonName(baseName, noRegion: true);
            var rgName = prefix + "-ip-pool-rg";

            using var testScope = new TestResourceGroupScope(rgName);

            var regions = new List<Region> { location, Region.USEast2 };
            IEnumerable<RegionOptions> regionOptions = GetRegionOptions(regions);

            var azFactory = new LiftrAzureFactory(testScope.Logger, TestCredentials.TenantId, TestCredentials.ObjectId, TestCredentials.SubscriptionId, TestCredentials.TokenCredential, TestCredentials.GetAzureCredentials);
            var az = azFactory.GenerateLiftrAzure();
            var pool = new IPPoolManager(prefix, true, azFactory, testScope.Logger);
            try
            {
                await az.GetOrCreateResourceGroupAsync(location, rgName, context.Tags);

                await pool.ProvisionIPPoolAsync(location, 5, context.Tags, regionOptions);
                var ipList = await pool.ListOutboundIPAsync(location);
                Assert.Equal(5, ipList.Count());

                ipList = await pool.ListInboundIPAsync(location);
                Assert.Equal(5, ipList.Count());

                ipList = await pool.ListOutboundIPAsync();
                Assert.Equal(10, ipList.Count());

                ipList = await pool.ListInboundIPAsync();
                Assert.Equal(10, ipList.Count());

                var inbound1 = await pool.GetAvailableInboundIPAsync(location);
                Assert.EndsWith("-01", inbound1.Name, StringComparison.Ordinal);
                Assert.Contains(nameof(IPCategory.Inbound), inbound1.Name, StringComparison.Ordinal);

                // test remove and recreate
                await az.DeleteResourceAsync(inbound1.Id, "2020-08-01");
                ipList = await pool.ListInboundIPAsync(location);
                Assert.Equal(4, ipList.Count());

                await pool.ProvisionIPPoolAsync(location, 5, context.Tags, regionOptions);
                ipList = await pool.ListInboundIPAsync(location);
                Assert.Equal(5, ipList.Count());

                inbound1 = await pool.GetAvailableInboundIPAsync(location);
                Assert.EndsWith("-01", inbound1.Name, StringComparison.Ordinal);
                Assert.Contains(nameof(IPCategory.Inbound), inbound1.Name, StringComparison.Ordinal);

                var outbound1 = await pool.GetAvailableOutboundIPAsync(location);
                Assert.EndsWith("-01", outbound1.Name, StringComparison.Ordinal);
                Assert.Contains(nameof(IPCategory.Outbound), outbound1.Name, StringComparison.Ordinal);

                // Test for not existing IPs
                ipList = await pool.ListOutboundIPAsync(Region.IndiaWest);
                Assert.Empty(ipList);

                var ipNotExist = await pool.GetAvailableInboundIPAsync(Region.UKSouth);
                Assert.Null(ipNotExist);

                var vnet = await az.GetOrCreateVNetAsync(location, rgName, "test-vnet", context.Tags);

                // occupy the 01 ip.
                var vm = await az.FluentClient.VirtualMachines.Define(SdkContext.RandomResourceName("vm", 3))
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .WithNewPrimaryNetwork("10.0.0.0/28")
                        .WithPrimaryPrivateIPAddressDynamic()
                        .WithExistingPrimaryPublicIPAddress(inbound1)
                        .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                        .WithRootUsername("test-user-123")
                        .WithSsh(s_sampleSSHPublic)
                        .WithSize(VirtualMachineSizeTypes.StandardDS2V2)
                        .WithTags(context.Tags)
                        .CreateAsync();

                var ip4 = await pool.GetAvailableInboundIPAsync(location);
                Assert.EndsWith("-02", ip4.Name, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                testScope.Logger.Error(ex, ex.Message);
                throw;
            }
            finally
            {
                _ = az.DeleteResourceGroupAsync(pool.OutboundIPPoolResourceGroupName);
                _ = az.DeleteResourceGroupAsync(pool.InboundIPPoolResourceGroupName);
                await Task.Delay(5000);
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task VerifyVMSSIPPoolAsync()
        {
            var shortPartnerName = "ip-pool";
            var location = TestCommon.Location;
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, location);
            TestCommon.AddCommonTags(context.Tags);

            var baseName = SdkContext.RandomResourceName("v", 3);
            var prefix = context.GenerateCommonName(baseName, noRegion: true);
            var rgName = prefix + "-ip-pool-rg";

            using var testScope = new TestResourceGroupScope(rgName);

            var regions = new List<Region> { location, Region.USEast2 };
            IEnumerable<RegionOptions> regionOptions = GetRegionOptions(regions);

            var azFactory = new LiftrAzureFactory(testScope.Logger, TestCredentials.TenantId, TestCredentials.ObjectId, TestCredentials.SubscriptionId, TestCredentials.TokenCredential, TestCredentials.GetAzureCredentials);
            var az = azFactory.GenerateLiftrAzure();
            var pool = new IPPoolManager(prefix, false, azFactory, testScope.Logger);
            try
            {
                await az.GetOrCreateResourceGroupAsync(location, rgName, context.Tags);

                await pool.ProvisionIPPoolAsync(location, 5, context.Tags, regionOptions);
                var ipList = await pool.ListOutboundIPAsync(location);
                Assert.Equal(5, ipList.Count());

                ipList = await pool.ListInboundIPAsync(location);
                Assert.Equal(5, ipList.Count());

                var inbound1 = await pool.GetAvailableInboundIPAsync(location);
                Assert.EndsWith("-01", inbound1.Name, StringComparison.Ordinal);
                Assert.DoesNotContain(nameof(IPCategory.Inbound), inbound1.Name, StringComparison.Ordinal);
                Assert.DoesNotContain(nameof(IPCategory.Outbound), inbound1.Name, StringComparison.Ordinal);
                Assert.DoesNotContain(nameof(IPCategory.InOutbound), inbound1.Name, StringComparison.Ordinal);

                var outbound1 = await pool.GetAvailableOutboundIPAsync(location);
                Assert.EndsWith("-01", outbound1.Name, StringComparison.Ordinal);
                Assert.DoesNotContain(nameof(IPCategory.Inbound), outbound1.Name, StringComparison.Ordinal);
                Assert.DoesNotContain(nameof(IPCategory.Outbound), outbound1.Name, StringComparison.Ordinal);
                Assert.DoesNotContain(nameof(IPCategory.InOutbound), outbound1.Name, StringComparison.Ordinal);

                // Test for not existing IPs
                ipList = await pool.ListOutboundIPAsync(Region.IndiaWest);
                Assert.Empty(ipList);

                var ipNotExist = await pool.GetAvailableInboundIPAsync(Region.UKSouth);
                Assert.Null(ipNotExist);

                var vnet = await az.GetOrCreateVNetAsync(location, rgName, "test-vnet", context.Tags);

                // occupy the 01 ip.
                var vm = await az.FluentClient.VirtualMachines.Define(SdkContext.RandomResourceName("vm", 3))
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .WithNewPrimaryNetwork("10.0.0.0/28")
                        .WithPrimaryPrivateIPAddressDynamic()
                        .WithExistingPrimaryPublicIPAddress(inbound1)
                        .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                        .WithRootUsername("test-user-123")
                        .WithSsh(s_sampleSSHPublic)
                        .WithSize(VirtualMachineSizeTypes.StandardDS2V2)
                        .WithTags(context.Tags)
                        .CreateAsync();

                var ip4 = await pool.GetAvailableInboundIPAsync(location);
                Assert.EndsWith("-02", ip4.Name, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                testScope.Logger.Error(ex, ex.Message);
                throw;
            }
            finally
            {
                _ = az.DeleteResourceGroupAsync(pool.OutboundIPPoolResourceGroupName);
                _ = az.DeleteResourceGroupAsync(pool.InboundIPPoolResourceGroupName);
                await Task.Delay(5000);
            }
        }

        private IEnumerable<RegionOptions> GetRegionOptions(IEnumerable<Region> regions)
        {
            List<RegionOptions> regionOptions = new List<RegionOptions>();

            foreach (var region in regions)
            {
                var regionOption = new RegionOptions
                {
                    Location = region,
                    ComputeBaseName = $"testCompute-{Guid.NewGuid()}",
                    DataBaseName = $"testDb-{Guid.NewGuid()}",
                };
                regionOptions.Add(regionOption);
            }

            return regionOptions;
        }
    }
}
