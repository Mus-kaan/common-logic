//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class HostingOptionsTests
    {
        [Fact]
        public void HostingOptionsParsing()
        {
            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.DomainName = null;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.Global = null;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.Regions = null;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.AKSConfigurations = null;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.OneCertCertificates = null;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.IPPerRegion = 2;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.IPPerRegion = 200;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }
        }

        private static HostingOptions GetAKSHostingOptions()
        {
            var fileName = "HostingOptions/aks-hosting-options.json";
            var expectedSerilized = "{\"partnerName\":\"Datadog\",\"shortPartnerName\":\"dd\",\"secretPrefix\":\"DatadogRP\",\"storageCountPerDataPlaneSubscription\":0,\"dbSupport\":true,\"enableThanos\":false,\"environments\":[{\"environmentName\":\"Dev\",\"azureSubscription\":\"eebfbfdb-4167-49f6-be43-466a6709609f\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"westus2\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false},{\"location\":\"eastus\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false}],\"oneCertCertificates\":{\"GenevaClientCert\":\"ib-rp-mds-agent.geneva.keyvault.liftr-dev.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-dev.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-dev.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-dev.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-dev.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-dev.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-dev.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.19.0\"},\"ipPerRegion\":3,\"enableVNet\":false,\"domainName\":\"dd-dev.azliftr-test.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-dev-wus-rg/providers/Microsoft.OperationalInsights/workspaces/sample-partner-log\",\"isAKS\":true},{\"environmentName\":\"DogFood\",\"azureSubscription\":\"d8f298fb-60f5-4676-a7d3-25442ec5ce1e\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"westus2\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false},{\"location\":\"eastus\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false},{\"location\":\"northeurope\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false}],\"oneCertCertificates\":{\"GenevaClientCert\":\"ib-rp-mds-agent.geneva.keyvault.liftr-dev.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-dev.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-dev.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-dev.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-dev.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-dev.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-dev.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.19.0\"},\"ipPerRegion\":3,\"enableVNet\":false,\"domainName\":\"dd-test.azliftr-test.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourcegroups/liftr-datadog-diagnotics-rg/providers/Microsoft.OperationalInsights/workspaces/liftr-ame-datadog-eus-la\",\"isAKS\":true},{\"environmentName\":\"Canary\",\"azureSubscription\":\"31d6176f-1b7a-44f3-9aed-cab9ba506ccd\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"eastus2\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false}],\"oneCertCertificates\":{\"GenevaClientCert\":\"datadog-mds-agent.geneva.keyvault.liftr-prod.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-prod.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-prod.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-prod.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-prod.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-prod.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-prod.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.19.0\"},\"ipPerRegion\":3,\"enableVNet\":false,\"domainName\":\"dd-canary.azliftr.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourcegroups/liftr-datadog-diagnotics-rg/providers/Microsoft.OperationalInsights/workspaces/liftr-ame-datadog-eus-la\",\"isAKS\":true},{\"environmentName\":\"Production\",\"azureSubscription\":\"31d6176f-1b7a-44f3-9aed-cab9ba506ccd\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"westcentralus\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false},{\"location\":\"westus2\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false},{\"location\":\"eastus\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"isSeparatedDataAndComputeRegion\":false}],\"oneCertCertificates\":{\"GenevaClientCert\":\"datadog-mds-agent.geneva.keyvault.liftr-prod.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-prod.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-prod.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-prod.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-prod.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-prod.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-prod.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.19.0\"},\"ipPerRegion\":3,\"enableVNet\":false,\"domainName\":\"dd-prod.azliftr.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourcegroups/liftr-datadog-diagnotics-rg/providers/Microsoft.OperationalInsights/workspaces/liftr-ame-datadog-eus-la\",\"isAKS\":true}]}";
            var options = JsonConvert.DeserializeObject<HostingOptions>(File.ReadAllText(fileName));
            options.CheckValid();
            var serialized = options.ToJsonString();
            Assert.Equal(expectedSerilized, serialized);

            return options;
        }

        private static HostingEnvironmentOptions GetDevAKSHostingOptions()
        {
            var options = GetAKSHostingOptions();

            var devOptions = options.Environments.FirstOrDefault(env => env.EnvironmentName == EnvironmentType.Dev);
            Assert.Equal("eebfbfdb-4167-49f6-be43-466a6709609f", devOptions.AzureSubscription.ToString());
            Assert.False(devOptions.EnableVNet);
            Assert.Equal(3, devOptions.IPPerRegion);
            Assert.Equal("dd-dev.azliftr-test.io", devOptions.DomainName);
            Assert.Equal("/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-dev-wus-rg/providers/Microsoft.OperationalInsights/workspaces/sample-partner-log", devOptions.LogAnalyticsWorkspaceId);
            Assert.True(devOptions.IsAKS);

            return devOptions;
        }
    }
}
