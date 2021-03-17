//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.KeyVault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class KeyVaultTests
    {
        private readonly ITestOutputHelper _output;

        public KeyVaultTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanCreateKeyVaultAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-kv-", _output))
            {
                try
                {
                    var azure = scope.Client;
                    var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("test-vault-", 15);
                    var kv = await azure.CreateKeyVaultAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);

                    await kv.Update()
                    .DefineAccessPolicy()
                    .ForServicePrincipal(TestCredentials.ClientId)
                    .AllowSecretAllPermissions()
                    .AllowCertificateAllPermissions()
                    .Attach()
                    .ApplyAsync();

                    // List
                    {
                        var resources = await azure.ListKeyVaultAsync(scope.ResourceGroupName);
                        Assert.Single(resources);
                        var r = resources.First();
                        Assert.Equal(name, r.Name);
                        TestCommon.CheckCommonTags(r.Inner.Tags);

                        Assert.Single(r.AccessPolicies);
                        Assert.Equal(TestCredentials.ObjectId, r.AccessPolicies[0].ObjectId);
                    }

                    // Get
                    {
                        var r = await azure.GetKeyVaultByIdAsync(kv.Id);
                        Assert.Equal(name, r.Name);
                        TestCommon.CheckCommonTags(r.Inner.Tags);

                        Assert.Single(r.AccessPolicies);
                        Assert.Equal(TestCredentials.ObjectId, r.AccessPolicies[0].ObjectId);
                    }
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanCreateKeyVaultInVNetAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-kv-vnet-", _output))
            {
                try
                {
                    var ip = await MetadataHelper.GetPublicIPAddressAsync();

                    var azure = scope.Client;
                    var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);

                    var vnet = await azure.GetOrCreateVNetAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("vnet", 9), TestCommon.Tags);
                    var subnet = vnet.Subnets.FirstOrDefault().Value;
                    var name = SdkContext.RandomResourceName("test-vault-", 15);

                    var kv = await azure.GetOrCreateKeyVaultAsync(TestCommon.Location, scope.ResourceGroupName, name, ip, TestCommon.Tags);
                    Assert.Equal(0, kv.AccessPolicies.Count);

                    await azure.GrantSelfKeyVaultAdminAccessAsync(kv);
                    Assert.Equal(1, kv.AccessPolicies.Count);
                    Assert.Equal(0, kv.Inner.Properties.NetworkAcls.VirtualNetworkRules.Count);
                    Assert.Equal(1, kv.Inner.Properties.NetworkAcls.IpRules.Count);

                    var newSubnet = await azure.CreateNewSubnetAsync(vnet, "new-subnet");

                    await azure.WithKeyVaultAccessFromNetworkAsync(kv, ip, newSubnet.Inner.Id);
                    kv = await kv.RefreshAsync();
                    Assert.Equal(1, kv.Inner.Properties.NetworkAcls.VirtualNetworkRules.Count);

                    await azure.WithKeyVaultAccessFromNetworkAsync(kv, ip, newSubnet.Inner.Id);
                    kv = await kv.RefreshAsync();
                    Assert.Equal(1, kv.Inner.Properties.NetworkAcls.VirtualNetworkRules.Count);

                    await azure.WithKeyVaultAccessFromNetworkAsync(kv, ip, subnet.Inner.Id);
                    kv = await kv.RefreshAsync();
                    Assert.Equal(2, kv.Inner.Properties.NetworkAcls.VirtualNetworkRules.Count);
                    Assert.Equal(1, kv.AccessPolicies.Count);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }

        [CheckInValidation(Skip = "Certificate creation is flacky recently.")]
        public async Task CanCreateCertificateInKeyVaultAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-kv-", _output))
            {
                try
                {
                    var azure = scope.Client;
                    var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("test-vault-", 15);
                    var kv = await azure.CreateKeyVaultAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);

                    await kv.Update()
                    .DefineAccessPolicy()
                    .ForServicePrincipal(TestCredentials.ClientId)
                    .AllowSecretAllPermissions()
                    .AllowCertificateAllPermissions()
                    .Attach()
                    .ApplyAsync();

                    // List
                    {
                        var resources = await azure.ListKeyVaultAsync(scope.ResourceGroupName);
                        Assert.Single(resources);
                        var r = resources.First();
                        Assert.Equal(name, r.Name);
                        TestCommon.CheckCommonTags(r.Inner.Tags);

                        Assert.Single(r.AccessPolicies);
                        Assert.Equal(TestCredentials.ObjectId, r.AccessPolicies[0].ObjectId);
                    }

                    // Get
                    {
                        var r = await azure.GetKeyVaultByIdAsync(kv.Id);
                        Assert.Equal(name, r.Name);
                        TestCommon.CheckCommonTags(r.Inner.Tags);

                        Assert.Single(r.AccessPolicies);
                        Assert.Equal(TestCredentials.ObjectId, r.AccessPolicies[0].ObjectId);
                    }

                    // Verify SSLAdmin OneCert creations.
                    using (var valet = new KeyVaultConcierge(kv.VaultUri, TestCredentials.ClientId, TestCredentials.ClientSecret, scope.Logger))
                    {
                        var certName = SdkContext.RandomResourceName("ssl", 8);
                        var subjectName = certName + ".azliftr-test.io";
                        var subjectAlternativeNames = new List<string>() { "*." + subjectName };
                        var certIssuerName = "one-cert-issuer";

                        await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                        await valet.CreateCertificateAsync(certName, certIssuerName, subjectName, subjectAlternativeNames, TestCommon.Tags);
                        var cert = await valet.GetCertAsync(certName);
                    }

                    // Verify AME OneCert creations.
                    using (var valet = new KeyVaultConcierge(kv.VaultUri, TestCredentials.ClientId, TestCredentials.ClientSecret, scope.Logger))
                    {
                        var certName = SdkContext.RandomResourceName("ame", 8);
                        var subjectName = certName + ".liftr-dev.net";
                        var subjectAlternativeNames = new List<string>() { "*." + subjectName };
                        var certIssuerName = "one-cert-issuer";

                        await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                        await valet.CreateCertificateAsync(certName, certIssuerName, subjectName, subjectAlternativeNames, TestCommon.Tags);
                        var cert = await valet.GetCertAsync(certName);
                    }

                    await azure.RemoveAccessPolicyAsync(kv.Id, TestCredentials.ObjectId);

                    // Get
                    {
                        var r = await azure.GetKeyVaultByIdAsync(kv.Id);
                        Assert.Equal(name, r.Name);
                        TestCommon.CheckCommonTags(r.Inner.Tags);

                        Assert.Empty(r.AccessPolicies);
                    }
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed");
                    throw;
                }
            }
        }
    }
}
