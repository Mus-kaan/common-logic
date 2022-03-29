//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Newtonsoft.Json;
using System;
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
                devOptions.IPPerRegion = 1;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }

            {
                var devOptions = GetDevAKSHostingOptions();
                devOptions.IPPerRegion = 25;
                Assert.Throws<InvalidHostingOptionException>(() =>
                {
                    devOptions.CheckValid();
                });
            }
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void AKSMachineTypeVerification()
        {
            var devOptions = GetDevAKSHostingOptions();
            devOptions.AKSConfigurations.AKSMachineType = null;

            try
            {
                devOptions.CheckValid();
            }
            catch (Exception ex)
            {
                if (ex.Message.OrdinalContains("AKSMachineType is not valid."))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Should throw");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void SelectAtLeastOneMachineConfiguration()
        {
            var devOptions = GetDevAKSHostingOptions();
            devOptions.AKSConfigurations.AKSMachineCount = null;
            devOptions.AKSConfigurations.AKSAutoScaleMinCount = null;

            try
            {
                devOptions.CheckValid();
            }
            catch (Exception ex)
            {
                if (ex.Message.OrdinalContains("Please provide machine count information through either specify a fixed"))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Should throw");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void SelectAtMostOneMachineConfiguration()
        {
            var devOptions = GetDevAKSHostingOptions();
            devOptions.AKSConfigurations.AKSMachineCount = 3;
            devOptions.AKSConfigurations.AKSAutoScaleMinCount = 3;
            devOptions.AKSConfigurations.AKSAutoScaleMaxCount = 5;

            try
            {
                devOptions.CheckValid();
            }
            catch (Exception ex)
            {
                if (ex.Message.OrdinalContains("Cannot support both fixed machine count and auto-scale. Please choose one"))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Should throw");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void InvalidAutoScaleMin()
        {
            var devOptions = GetDevAKSHostingOptions();
            devOptions.AKSConfigurations.AKSMachineCount = null;
            devOptions.AKSConfigurations.AKSAutoScaleMinCount = 1;
            devOptions.AKSConfigurations.AKSAutoScaleMaxCount = 6;

            try
            {
                devOptions.CheckValid();
            }
            catch (Exception ex)
            {
                if (ex.Message.OrdinalContains("AKSAutoScaleMinCount should >= 2."))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Should throw");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void InvalidAutoScaleMax()
        {
            var devOptions = GetDevAKSHostingOptions();
            devOptions.AKSConfigurations.AKSMachineCount = null;
            devOptions.AKSConfigurations.AKSAutoScaleMinCount = 4;
            devOptions.AKSConfigurations.AKSAutoScaleMaxCount = 600;

            try
            {
                devOptions.CheckValid();
            }
            catch (Exception ex)
            {
                if (ex.Message.OrdinalContains("AKSAutoScaleMaxCount should <= 200"))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Should throw");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public void ConflictAutoScaleMinAndMax()
        {
            var devOptions = GetDevAKSHostingOptions();
            devOptions.AKSConfigurations.AKSMachineCount = null;
            devOptions.AKSConfigurations.AKSAutoScaleMinCount = 7;
            devOptions.AKSConfigurations.AKSAutoScaleMaxCount = 6;

            try
            {
                devOptions.CheckValid();
            }
            catch (Exception ex)
            {
                if (ex.Message.OrdinalContains("AKSAutoScaleMinCount should <= AKSAutoScaleMaxCount"))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Should throw");
        }

        private static HostingOptions GetAKSHostingOptions()
        {
            var fileName = "HostingOptions/aks-hosting-options.json";

            var expectedSerilized = "{\"partnerName\":\"Datadog\",\"shortPartnerName\":\"dd\",\"secretPrefix\":\"DatadogRP\",\"storageCountPerDataPlaneSubscription\":0,\"dbSupport\":true,\"enableThanos\":false,\"enableLiftrCommonImages\":false,\"environments\":[{\"environmentName\":\"Dev\",\"azureSubscription\":\"eebfbfdb-4167-49f6-be43-466a6709609f\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"westus2\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true},{\"location\":\"eastus\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true}],\"oneCertCertificates\":{\"GenevaClientCert\":\"ib-rp-mds-agent.geneva.keyvault.liftr-dev.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-dev.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-dev.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-dev.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-dev.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-dev.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-dev.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.21.7\"},\"partnerCredentialUpdateConfig\":{\"multiTenantAppId\":\"a3b7c1aa-e4d7-40c0-a480-187679dec0b2\",\"partnerKeyvaultEndpoint\":\"https://confluent-liftr-kv.vault.azure.net\",\"certificateSubjectName\":\"partner-cred-secure-sharing.token-svc.liftr-dev.net\"},\"ipPerRegion\":3,\"enableVNet\":false,\"enablePromIcM\":true,\"domainName\":\"dd-dev.azliftr-test.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourceGroups/liftr-dev-wus-rg/providers/Microsoft.OperationalInsights/workspaces/sample-partner-log\",\"isAKS\":true},{\"environmentName\":\"DogFood\",\"azureSubscription\":\"d8f298fb-60f5-4676-a7d3-25442ec5ce1e\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"westus2\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true},{\"location\":\"eastus\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true},{\"location\":\"northeurope\",\"dataBaseName\":\"data20200502\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true}],\"oneCertCertificates\":{\"GenevaClientCert\":\"ib-rp-mds-agent.geneva.keyvault.liftr-dev.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-dev.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-dev.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-dev.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-dev.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-dev.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-dev.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.21.7\"},\"ipPerRegion\":3,\"enableVNet\":false,\"enablePromIcM\":true,\"domainName\":\"dd-test.azliftr-test.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourcegroups/liftr-datadog-diagnotics-rg/providers/Microsoft.OperationalInsights/workspaces/liftr-ame-datadog-eus-la\",\"isAKS\":true},{\"environmentName\":\"Canary\",\"azureSubscription\":\"31d6176f-1b7a-44f3-9aed-cab9ba506ccd\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"eastus2\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true}],\"oneCertCertificates\":{\"GenevaClientCert\":\"datadog-mds-agent.geneva.keyvault.liftr-prod.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-prod.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-prod.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-prod.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-prod.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-prod.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-prod.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.21.7\"},\"ipPerRegion\":3,\"enableVNet\":false,\"enablePromIcM\":true,\"domainName\":\"dd-canary.azliftr.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourcegroups/liftr-datadog-diagnotics-rg/providers/Microsoft.OperationalInsights/workspaces/liftr-ame-datadog-eus-la\",\"isAKS\":true},{\"environmentName\":\"Production\",\"azureSubscription\":\"31d6176f-1b7a-44f3-9aed-cab9ba506ccd\",\"global\":{\"location\":\"centralus\",\"baseName\":\"gbl202001001\",\"addGlobalDB\":false},\"regions\":[{\"location\":\"westcentralus\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":false},{\"location\":\"westus2\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true},{\"location\":\"eastus\",\"dataBaseName\":\"data20200602\",\"computeBaseName\":\"com20200727\",\"zoneRedundant\":false,\"supportAvailabilityZone\":true}],\"oneCertCertificates\":{\"GenevaClientCert\":\"datadog-mds-agent.geneva.keyvault.liftr-prod.net\",\"FirstPartyAppCert\":\"liftr-monitoring.first-party.liftr-prod.net\",\"DatadogFirstPartyAppCert\":\"datadog-rp.first-party.liftr-prod.net\",\"DatadogMPCert\":\"market-place.datadog.liftr-prod.net\",\"DatadogRPaaSCert\":\"datadog-rpaas.first-party.liftr-prod.net\",\"TokenServicePrimaryCert\":\"primary-signing.token-svc.liftr-prod.net\",\"TokenServiceSecondaryCert\":\"secondary-signing.token-svc.liftr-prod.net\"},\"aksConfigurations\":{\"aksMachineCount\":3,\"aksMachineType\":\"Standard_DS2_v2\",\"kubernetesVersion\":\"1.21.7\"},\"ipPerRegion\":3,\"enableVNet\":false,\"enablePromIcM\":true,\"domainName\":\"dd-prod.azliftr.io\",\"logAnalyticsWorkspaceId\":\"/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourcegroups/liftr-datadog-diagnotics-rg/providers/Microsoft.OperationalInsights/workspaces/liftr-ame-datadog-eus-la\",\"isAKS\":true}]}";
            var options = JsonConvert.DeserializeObject<HostingOptions>(File.ReadAllText(fileName));

            // options.CheckValid();
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
            Assert.Equal("a3b7c1aa-e4d7-40c0-a480-187679dec0b2", devOptions.PartnerCredentialUpdateConfig.MultiTenantAppId);

            return devOptions;
        }
    }
}
