//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Monitoring.Whale.Poseidon.Tests
{
    /// <summary>
    /// Tests for the WhaleFilterClient class.
    /// </summary>
    public class PoseidonFilterClientTests
    {
        private readonly TestResourcesFactory _resourcesFactory;
        private readonly IWhaleFilterClient _whaleFilterClient;

        public PoseidonFilterClientTests()
        {
            var clientsProvider = new PoseidonClientsProvider();
            var loggerMock = new Mock<ILogger>();

            _resourcesFactory = new TestResourcesFactory();
            _whaleFilterClient = new WhaleFilterClient(clientsProvider, loggerMock.Object);
        }

        /// <summary>
        /// On this test, we ensure that the ouputs from the Resource Graph Client are the
        /// expected values, matching the ID and the location of the test resources.
        /// </summary>
        [Fact(Skip = "For local testing")]
        public async Task ListResourcesByTagsAsync_WithExistingResources_ReturnsExpectedValuesAsync()
        {
            // Create test resources and filtering tags
            await _resourcesFactory.CreateResoucesAsync();
            var filteringTags = new List<FilteringTag>();
            filteringTags.Add(PoseidonConstants.InclusionFilteringTag);

            // Sleep for 2 mins to make sure latest changes will show up in ARG query
            await Task.Delay(TimeSpan.FromMinutes(2));

            // First test with only the included tag. Both IPs should be returned.
            // The resources should be in West US 2 region.
            var resourcesList = await _whaleFilterClient.ListResourcesByTagsAsync(
                TestCredentials.SubscriptionId, TestCredentials.TenantId, filteringTags);

            Assert.True(resourcesList.Count() == 2);
            Assert.Contains(resourcesList, r => r.Id.OrdinalEquals(PoseidonConstants.PublicIp1Id));
            Assert.Contains(resourcesList, r => r.Id.OrdinalEquals(PoseidonConstants.PublicIp2Id));
            Assert.DoesNotContain(resourcesList, r => !r.Location.OrdinalEquals(PoseidonConstants.TestLocationName));

            // Second test with both inclusion and exclusion tags. Only IP 1 should be returned.
            // Resources should still be in West US 2 region.
            filteringTags.Add(PoseidonConstants.ExclusionFilteringTag);
            resourcesList = await _whaleFilterClient.ListResourcesByTagsAsync(
                TestCredentials.SubscriptionId, TestCredentials.TenantId, filteringTags);

            Assert.Single(resourcesList);
            Assert.Contains(resourcesList, r => r.Id.OrdinalEquals(PoseidonConstants.PublicIp1Id));
            Assert.DoesNotContain(resourcesList, r => !r.Location.OrdinalEquals(PoseidonConstants.TestLocationName));

            await _resourcesFactory.DeleteResourcesAsync();
        }

        /// <summary>
        /// Script to delete Datadog diagnostic settings from resources.
        /// To be used on the incapacity of the whale to delete diagnostic settings from a given subscription.
        /// This is mainly for internal subscriptions. Fill in the TODOs below with the necessary data.
        /// </summary>
        /// <returns></returns>
        [Fact(Skip = "For manual clean-up only")]
        public async Task CleanUpResidualDiagnosticSettingsAsync()
        {
            // TODO: use a set of credentials that has, at least, monitoring contributor role in the listed subscriptions.
            // WARNING: Rollback after using. Don't commit the credentials.
            var clientId = string.Empty;
            var clientSecret = string.Empty;
            var tenantId = string.Empty;

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            // TODO: add the list of subscriptions here. The application being used
            // must have, at least, monitoring contributor role in the listed values.
            // WARNING: Rollback after using. Don't commit the subscriptions.
            var subscriptions = new List<string>()
            {
            };

            var resourcesWithDatadogDiagnosticSettings = new List<string>();

            foreach (var subscription in subscriptions)
            {
                var resourceManagerClient = ResourceManager.Authenticate(credentials).WithSubscription(subscription);
                var fluentClient = Azure.Management.Fluent.Azure.Authenticate(credentials).WithSubscription(subscription);

                var resources = await resourceManagerClient.GenericResources.ListAsync();

                foreach (var resource in resources)
                {
                    try
                    {
                        var existingDiagnosticSettings = await fluentClient.DiagnosticSettings.ListByResourceAsync(resource.Id);
                        var datadogDiagnosticSettings = existingDiagnosticSettings.Where(ds => ds.Name.OrdinalStartsWith("DATADOG_DS_"));

                        if (datadogDiagnosticSettings.Any())
                        {
                            resourcesWithDatadogDiagnosticSettings.Add(resource.Id);
                        }

                        foreach (var diagnosticSetting in datadogDiagnosticSettings)
                        {
                            await fluentClient.DiagnosticSettings.DeleteByIdAsync(diagnosticSetting.Id);
                        }
                    }
                    catch (Exception)
                    {
                        // Don't block logic as some resources will not have diagnostic settings and will fail
                    }
                }
            }

            // TODO: Add a breakpoint here. Re-run the test until this list becomes empty.
            var listOfAffectedResources = resourcesWithDatadogDiagnosticSettings.ToJson();
            Console.WriteLine(listOfAffectedResources);
        }
    }
}
