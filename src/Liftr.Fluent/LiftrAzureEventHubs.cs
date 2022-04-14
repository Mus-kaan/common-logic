//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Eventhub.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region Event Hub
        public async Task<IEventHubNamespace> GetOrCreateEventHubNamespaceAsync(Region location, string rgName, string name, int throughtputUnits, int maxThroughtputUnits, IDictionary<string, string> tags)
        {
            _logger.Information("Getting Event hub namespace. rgName: {rgName}, name: {name} ...", rgName, name);
            IEventHubNamespace eventHubNamespace = null;
            try
            {
                eventHubNamespace = await FluentClient
                    .EventHubNamespaces
                    .GetByResourceGroupAsync(rgName, name);
            }
            catch (Azure.Management.EventHub.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Creating a Event hub namespace. rgName: {rgName}, name: {name} ...", rgName, name);
                eventHubNamespace = await FluentClient
                    .EventHubNamespaces
                    .Define(name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithAutoScaling()
                    .WithCurrentThroughputUnits(throughtputUnits)
                    .WithThroughputUnitsUpperLimit(maxThroughtputUnits)
                    .WithTags(tags)
                    .CreateAsync();
            }

            return eventHubNamespace;
        }

        public async Task<IEventHub> GetOrCreateEventHubAsync(Region location, string rgName, string namespaceName, string hubName, int partitionCount, int throughtputUnits, int maxThroughtputUnits, IList<string> consumerGroups, IDictionary<string, string> tags)
        {
            _logger.Information("Getting Event Hub. rgName: {rgName}, namespaceName: {namespaceName}, hubName: {hubName} ...", rgName, namespaceName, hubName);
            IEventHub eventhub = null;

            if (consumerGroups == null)
            {
                throw new ArgumentNullException(nameof(consumerGroups));
            }

            try
            {
                eventhub = await FluentClient
                    .EventHubs
                    .GetByNameAsync(rgName, namespaceName, hubName);
            }
            catch (Azure.Management.EventHub.Fluent.Models.ErrorResponseException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Information("Cannot find Event Hub. rgName: {rgName}, namespaceName: {namespaceName}, hubName: {hubName} ...", rgName, namespaceName, hubName);
                IEventHubNamespace eventHubNamespace = await GetOrCreateEventHubNamespaceAsync(location, rgName, namespaceName, throughtputUnits, maxThroughtputUnits, tags);

                _logger.Information($"Creating a Event Hub with namespaceName {namespaceName}, name {hubName} ...", namespaceName, hubName);

                var eventHubBuilder = FluentClient
                    .EventHubs
                    .Define(hubName)
                    .WithExistingNamespace(eventHubNamespace)
                    .WithPartitionCount(partitionCount);

                foreach (var consumerGroup in consumerGroups)
                {
                    eventHubBuilder.WithNewConsumerGroup(consumerGroup);
                }

                eventhub = await eventHubBuilder.CreateAsync();
            }

            return eventhub;
        }

        public async Task<IEventHub> GetEventHubAsync(string rgName, string namespaceName, string hubName)
        {
            var eventhub = await FluentClient
                    .EventHubs
                    .GetByNameAsync(rgName, namespaceName, hubName);

            return eventhub;
        }

        public async Task GrantEventHubRoleAsync(string objectId, IEventHub eventHub, BuiltInRole role, CancellationToken cancellationToken = default)
        {
            try
            {
                await Authenticated.RoleAssignments
                              .Define(SdkContext.RandomGuid())
                              .ForObjectId(objectId)
                              .WithBuiltInRole(role)
                              .WithScope(eventHub.Id)
                              .CreateAsync(cancellationToken);
                _logger.Information("Granted 'Azure Event Hubs Data Receiver' for eventhub '{resourceId}' to SPN with object Id {objectId}.", eventHub.Id, objectId);
            }
            catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
            {
            }
            catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
            {
                _logger.Error("The object Id '{objectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", objectId);
                throw;
            }
        }

        public async Task GrantEventHubReceiverRoleAsync(string objectId, IEventHub eventHub, CancellationToken cancellationToken = default)
        {
            var eventHubReceiverRole = BuiltInRole.Parse("Azure Event Hubs Data Receiver");
            await GrantEventHubRoleAsync(objectId, eventHub, eventHubReceiverRole, cancellationToken);
        }

        public async Task GrantEventHubSenderRoleAsync(string objectId, IEventHub eventHub, CancellationToken cancellationToken = default)
        {
            var eventHubSenderRole = BuiltInRole.Parse("Azure Event Hubs Data Sender");
            await GrantEventHubRoleAsync(objectId, eventHub, eventHubSenderRole, cancellationToken);
        }

        public async Task AddConsumerGroupAsync(string consumerGroupName, IEventHub eventHub, CancellationToken cancellationToken = default)
        {
            await eventHub
            .Update()
            .WithNewConsumerGroup(consumerGroupName)
            .ApplyAsync();

            _logger.Information("Added consumer group: {consumerGroupName} to eventhub: {resourceId}.", consumerGroupName, eventHub.Id);
        }
        #endregion
    }
}
