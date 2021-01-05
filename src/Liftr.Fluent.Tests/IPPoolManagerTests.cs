//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.Hosting.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public async Task VerifyProvisioningIPPoolAsync()
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

            try
            {
                var clientFactory = new LiftrAzureFactory(testScope.Logger, TestCredentials.TenantId, TestCredentials.ObjectId, TestCredentials.SubscriptionId, TestCredentials.TokenCredential, TestCredentials.GetAzureCredentials);
                var client = clientFactory.GenerateLiftrAzure();

                var pool = new IPPoolManager(prefix, clientFactory, testScope.Logger);

                await pool.ProvisionIPPoolAsync(location, 5, new Dictionary<string, string>() { { "env", "test" } }, false, regionOptions);
                var allIPs = await pool.ListAllIPAsync(location);
                Assert.Equal(5, allIPs.Count());

                var ip1 = await pool.GetAvailableIPAsync(location);
                Assert.EndsWith("-01", ip1.Name, StringComparison.Ordinal);

                await pool.ProvisionIPPoolAsync(location, 1, new Dictionary<string, string>() { { "env", "test" } }, true, regionOptions);
                allIPs = await pool.ListAllIPAsync(location, IPCategory.Inbound);
                Assert.Single(allIPs);

                var ip2 = await pool.GetAvailableIPAsync(location, IPCategory.Inbound);
                var ip2Name = ip2.Name;
                Assert.Contains(IPCategory.Inbound.ToString(), ip2Name, StringComparison.Ordinal);

                allIPs = await pool.ListAllIPAsync(location, IPCategory.Outbound);
                Assert.Single(allIPs);

                var ip3 = await pool.GetAvailableIPAsync(location, IPCategory.Outbound);
                var ip3Name = ip3.Name;
                Assert.Contains(IPCategory.Outbound.ToString(), ip3Name, StringComparison.Ordinal);

                // Test for not existing IPs
                await pool.ProvisionIPPoolAsync(Region.USEast2, 1, new Dictionary<string, string>() { { "env", "test" } }, true, regionOptions);
                allIPs = await pool.ListAllIPAsync(Region.USEast2, IPCategory.Inbound);
                Assert.Single(allIPs);

                var ipNotExist = await pool.GetAvailableIPAsync(Region.UKSouth, IPCategory.Inbound);
                Assert.Null(ipNotExist);

                var vnet = await client.GetOrCreateVNetAsync(location, rgName, "test-vnet", context.Tags);

                // occupy the 01 ip.
                var vm = await client.FluentClient.VirtualMachines.Define(SdkContext.RandomResourceName("vm", 3))
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .WithNewPrimaryNetwork("10.0.0.0/28")
                        .WithPrimaryPrivateIPAddressDynamic()
                        .WithExistingPrimaryPublicIPAddress(ip1)
                        .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                        .WithRootUsername("test-user-123")
                        .WithSsh(s_sampleSSHPublic)
                        .WithSize(VirtualMachineSizeTypes.StandardDS2V2)
                        .WithTags(context.Tags)
                        .CreateAsync();

                var ip4 = await pool.GetAvailableIPAsync(location);
                Assert.EndsWith("-02", ip4.Name, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                testScope.Logger.Error(ex, ex.Message);
                throw;
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
