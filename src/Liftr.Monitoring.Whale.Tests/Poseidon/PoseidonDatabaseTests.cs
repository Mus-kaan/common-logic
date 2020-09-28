//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Monitoring.Whale.Poseidon.Tests
{
    /// <summary>
    /// The tests on this class will ensure that the Database is set up for tests,
    /// meaning that the event hub data source contains a valid entity, mapping
    /// to an existing event hub at West US 2 location.
    /// </summary>
    public class PoseidonDatabaseTests
    {
        private readonly IAzureClientsProvider _clientsProvider;

        public PoseidonDatabaseTests()
        {
            _clientsProvider = new PoseidonClientsProvider();
        }

        /// <summary>
        /// Ensure event hub data source contains a valid entity.
        /// </summary>
        [Fact(Skip = "For local testing")]
        public async Task EventHubDataSource_EventHubEntity_EnsureEntityExistsAsync()
        {
            var eventHubDataSource = await PoseidonDatabaseProvider.GetEventHubDataSourceAsync();
            var eventHubEntities = await eventHubDataSource.ListAsync(
              MonitoringResourceProvider.Datadog, PoseidonConstants.TestLocationName);

            if (!eventHubEntities.Any())
            {
                var newEntity = await GetEntityToAddToDatabaseAsync();
                await eventHubDataSource.AddAsync(newEntity);
                eventHubEntities = await eventHubDataSource.ListAsync(
                   MonitoringResourceProvider.Datadog, PoseidonConstants.TestLocationName);
            }

            var eventHubEntity = eventHubEntities.First();

            Assert.Equal(PoseidonConstants.EventHubName, eventHubEntity.Name);
            Assert.Equal(PoseidonConstants.EventHubNamespaceName, eventHubEntity.Namespace);
        }

        /// <summary>
        /// Ensure the event hub entity at the database maps to
        /// an existing event hub with the expected properties.
        /// </summary>
        [Fact(Skip = "For local testing")]
        public async Task EventHubDataSource_EventHubEntity_EnsureResourceExistsAsync()
        {
            var fluentClient = await _clientsProvider.GetFluentClientAsync(TestCredentials.SubscriptionId, TestCredentials.TenantId);
            var eventHubNamespace = await fluentClient.EventHubNamespaces
                .GetByResourceGroupAsync(
                    PoseidonConstants.TestResourceGroup,
                    PoseidonConstants.EventHubNamespaceName);

            Assert.Equal(Region.USWest2, eventHubNamespace.Region);

            var eventHubs = await eventHubNamespace.ListEventHubsAsync();
            Assert.Contains(eventHubs, e => e.Name.OrdinalEquals(PoseidonConstants.EventHubName));

            var authorizationRules = await eventHubNamespace.ListAuthorizationRulesAsync();
            Assert.Contains(authorizationRules, a => a.Name.OrdinalEquals(
                PoseidonConstants.EventHubAuthorizationRuleName));
        }

        /// <summary>
        /// Ensure partner data source contains entities for Datadog resources 1 and 2 exist.
        /// </summary>
        [Fact(Skip = "For local testing")]
        public async Task PartnerDataSource_PartnerResourceEntity_EnsureEntityExistsAsync()
        {
            var partnerDataSource = await PoseidonDatabaseProvider.GetPartnerDataSourceAsync();

            var existingEntity1 = await partnerDataSource.GetAsync(PoseidonConstants.DatadogEntityId1);
            if (existingEntity1 == null)
            {
                var newEntity1 = new PartnerResourceEntity()
                {
                    EntityId = PoseidonConstants.DatadogEntityId1,
                    ResourceId = PoseidonConstants.DatadogMonitorId1,
                    ResourceType = "Microsoft.Datadog/monitors",
                    EncryptedContent = ConfigurationLoader.GetAPIKey(),
                    TenantId = TestCredentials.TenantId,
                    EncryptionKeyResourceId = string.Empty,
                    EncryptionAlgorithm = EncryptionAlgorithm.A256CBC,
                    ContentEncryptionIV = null,
                    Active = true,
                    ProvisioningState = ProvisioningState.Succeeded,
                };

                await partnerDataSource.AddAsync(newEntity1);
            }

            var existingEntity2 = await partnerDataSource.GetAsync(PoseidonConstants.DatadogEntityId2);
            if (existingEntity2 == null)
            {
                var newEntity2 = new PartnerResourceEntity()
                {
                    EntityId = PoseidonConstants.DatadogEntityId2,
                    ResourceId = PoseidonConstants.DatadogMonitorId2,
                    ResourceType = "Microsoft.Datadog/monitors",
                    EncryptedContent = ConfigurationLoader.GetAPIKey(),
                    TenantId = TestCredentials.TenantId,
                    EncryptionKeyResourceId = string.Empty,
                    EncryptionAlgorithm = EncryptionAlgorithm.A256CBC,
                    ContentEncryptionIV = null,
                    Active = true,
                    ProvisioningState = ProvisioningState.Succeeded,
                };

                await partnerDataSource.AddAsync(newEntity2);
            }
        }

        private async Task<EventHubEntity> GetEntityToAddToDatabaseAsync()
        {
            var fluentClient = await _clientsProvider.GetFluentClientAsync(TestCredentials.SubscriptionId, TestCredentials.TenantId);
            var eventHubNamespace = await fluentClient.EventHubNamespaces
                .GetByResourceGroupAsync(
                    PoseidonConstants.TestResourceGroup,
                    PoseidonConstants.EventHubNamespaceName);

            var authRules = await eventHubNamespace.ListAuthorizationRulesAsync();
            var authRule = authRules
                .Where(a => a.Name.OrdinalEquals(PoseidonConstants.EventHubAuthorizationRuleName))
                .First();

            var keys = await authRule.GetKeysAsync();

            var entityToAdd = new EventHubEntity()
            {
                ResourceProvider = MonitoringResourceProvider.Datadog,
                Namespace = PoseidonConstants.EventHubNamespaceName,
                Name = PoseidonConstants.EventHubName,
                Location = PoseidonConstants.TestLocationName,
                EventHubConnectionString = keys.PrimaryConnectionString,
                StorageConnectionString = keys.PrimaryConnectionString,
                AuthorizationRuleId = authRule.Id,
            };

            return entityToAdd;
        }
    }
}
