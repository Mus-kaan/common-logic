//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
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

        [Fact]
        public async Task CanCreateKeyVaultAsync()
        {
            var logger = TestLogger.GetLogger(_output);
            using (var scope = new TestResourceGroupScope("unittest-kv-", _output))
            {
                var azure = scope.Client;
                var rg = await azure.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("test-vault-", 15);
                var kv = await azure.CreateKeyVaultAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, TestCredentials.ClientId);

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

                // Verify AME OneCert creations.
                using (var valet = new KeyVaultConcierge(kv.VaultUri, TestCredentials.ClientId, TestCredentials.ClientSecret, logger))
                {
                    var certName = SdkContext.RandomResourceName("ame", 8);
                    var subjectName = certName + ".liftr-dev.net";
                    var subjectAlternativeNames = new List<string>() { "*." + subjectName };
                    var certIssuerName = "one-cert-issuer";

                    await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                    await valet.CreateCertificateAsync(certName, certIssuerName, subjectName, subjectAlternativeNames, TestCommon.Tags);
                    var cert = await valet.DownloadCertAsync(certName);
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
        }
    }
}
