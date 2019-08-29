//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class WebAppTests
    {
        private readonly ITestOutputHelper _output;

        public WebAppTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateWebAppAsync()
        {
            var logger = TestLogger.GetLogger(_output);
            using (var scope = new TestResourceGroupScope("unittest-antares-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var name = SdkContext.RandomResourceName("test-web-app-", 15);
                var envName = "Development";
                var created = await client.CreateWebAppAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, PricingTier.StandardS1, envName);

                var resources = await client.ListWebAppAsync(scope.ResourceGroupName);
                Assert.Single(resources);

                var webApp = resources.First();
                Assert.Equal(name, webApp.Name);
                TestCommon.CheckCommonTags(webApp.Inner.Tags);

                var kv = await client.CreateKeyVaultAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("test-vault-", 15), TestCommon.Tags, TestCredentials.ClientId);

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

                    await client.UploadCertificateToWebAppAsync(webApp.Id, "test-cert", Convert.FromBase64String(cert.Value));
                }

                var appServicePlan = await client.GetAppServicePlanByIdAsync(webApp.AppServicePlanId);
            }
        }
    }
}
