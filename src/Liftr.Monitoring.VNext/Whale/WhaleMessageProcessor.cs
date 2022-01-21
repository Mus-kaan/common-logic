//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.Whale;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.Whale.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Microsoft.Liftr.RPaaS;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitoringStatus = Microsoft.Liftr.Monitoring.VNext.Whale.Models.MonitoringStatus;

namespace Microsoft.Liftr.Monitoring.VNext.Whale
{
    /// <summary>
    /// Message processor for whale service to update log and metrics filter rules.
    /// </summary>
    public class WhaleMessageProcessor : IWhaleMessageProcessor
    {
        private readonly IMetaRPWhaleService _metaRPWhaleService;
        private readonly IWhaleFilterClient _whaleClient;
        private readonly IMetricsRulesUpdateService _metricsRulesUpdateService;
        private readonly IMonitoredResourceManager _resourceManager;
        private readonly IPartnerResourceDataSource<PartnerResourceEntity> _partnerDataSource;
        private readonly ILogger _logger;

        public WhaleMessageProcessor(
            IMetaRPWhaleService metaRPWhaleService,
            IWhaleFilterClient whaleFilterClient,
            IMetricsRulesUpdateService metricsRulesUpdateService,
            IMonitoredResourceManager resourceManager,
            IPartnerResourceDataSource<PartnerResourceEntity> partnerDataSource,
            ILogger logger)
        {
            _metaRPWhaleService = metaRPWhaleService ?? throw new ArgumentNullException(nameof(metaRPWhaleService));
            _whaleClient = whaleFilterClient ?? throw new ArgumentNullException(nameof(whaleFilterClient));
            _metricsRulesUpdateService = metricsRulesUpdateService ?? throw new ArgumentNullException(nameof(metricsRulesUpdateService));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _partnerDataSource = partnerDataSource ?? throw new ArgumentNullException(nameof(partnerDataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessUpdateTagRulesMessageAsync(string monitorId, string tenantId)
        {
            if (monitorId == null)
            {
                throw new ArgumentNullException(nameof(monitorId));
            }

            if (tenantId == null)
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            _logger.Information("Processing update tag rules message for resource {monitorId}.", monitorId);

            var tagRules = await _metaRPWhaleService.GetMonitoringTagRulesAsync(monitorId, tenantId);

            var monitorResource = await _metaRPWhaleService.GetMonitorResourceDetailsAsync(monitorId, tenantId);

            await _metricsRulesUpdateService.UpdateMetricRulesAsync(monitorId, tenantId);

            _logger.Information(
                "Updating log rules for monitor {monitorId} with entity {partnerEntityId}; new log rules are {@logRules}. Monitoring status is {monitoringStatus}.",
                monitorId,
                monitorResource.MonitoringPartnerEntityId,
                tagRules.Properties.LogRules,
                Enum.GetName(typeof(MonitoringStatus), monitorResource.MonitoringStatus));

            var monitorLocation = monitorResource.Location
                        .Replace(" ", string.Empty).ToLowerInvariant();

            await UpdateLogRulesAsync(
                tagRules,
                monitorId,
                monitorResource.MonitoringPartnerEntityId,
                monitorResource.MonitoringStatus,
                tenantId,
                monitorLocation);

            _logger.Information(
                "Updating subscription status for monitor {monitorId} with entity {partnerEntityId}; new status for subscription logs is {@sendSubscriptionLogs}. Monitoring status is {monitoringStatus}.",
                monitorId,
                monitorResource.MonitoringPartnerEntityId,
                tagRules.Properties.LogRules.SendSubscriptionLogs,
                Enum.GetName(typeof(MonitoringStatus), monitorResource.MonitoringStatus));

            if (tagRules.Properties.LogRules.SendSubscriptionLogs
                && monitorResource.MonitoringStatus == MonitoringStatus.Enabled)
            {
                // Send logs only if subscription logs are enabled and monitor resource is enabled
                await _resourceManager.StartMonitoringSubscriptionAsync(
                    monitorId, monitorResource.Location, monitorResource.MonitoringPartnerEntityId, tenantId);
            }
            else
            {
                // Do not send logs of conditions are not met
                await _resourceManager.StopMonitoringSubscriptionAsync(
                    monitorId, monitorResource.MonitoringPartnerEntityId, tenantId);
            }
        }

        public async Task<ProvisioningState> ProcessAutoMonitoringMessageAsync(string partnerEntityId, string tenantId)
        {
            if (partnerEntityId == null)
            {
                throw new ArgumentNullException(nameof(partnerEntityId));
            }

            if (tenantId == null)
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var entity = await _partnerDataSource.GetAsync(partnerEntityId);

            if (entity == null)
            {
                // If the entity is null here, then the resource has been deleted
                _logger.Information($"Cannot process auto-monitoring as entity with id {partnerEntityId} has been deleted.");
                return ProvisioningState.Deleted;
            }

            var monitorId = entity.ResourceId;

            MonitoringTagRules tagRules = null;
            MonitorResourceDetails monitorResource = null;

            // First, check if monitor resource has been deleted
            try
            {
                monitorResource = await _metaRPWhaleService.GetMonitorResourceDetailsAsync(monitorId, tenantId);
            }
            catch (MetaRPException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.Information(
                        "Monitor resource {@monitorId} with entity {partnerEntityId} is deleted.", monitorId, partnerEntityId);

                    return ProvisioningState.Deleted;
                }

                throw;
            }

            // Second, check if monitor resource is being deleted or is in a non-active state
            if (monitorResource.ProvisioningState.ShouldStopMonitoringResource()
                || monitorResource.ProvisioningState.ShouldNotStartMonitoringResource())
            {
                _logger.Information(
                    "Monitor resource {monitorId} with entity {partnerEntityId} is in a non-active state.",
                    monitorId,
                    partnerEntityId);

                return monitorResource.ProvisioningState;
            }

            try
            {
                // Capture existing rules
                tagRules = await _metaRPWhaleService.GetMonitoringTagRulesAsync(monitorId, tenantId);
            }
            catch (MetaRPException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.Information(
                        "TagRules for resource {monitorId} with entity {partnerEntityId} have not been created yet.",
                        monitorId,
                        partnerEntityId);

                    // Continue auto-monitoring resource, even though it doesn't have tag rules yet
                    return ProvisioningState.Creating;
                }

                throw;
            }

            _logger.Information(
                "Processing auto-monitor message for resource {@monitorId} with entity {partnerEntityId}.",
                monitorId,
                partnerEntityId);

            _logger.Information(
                $"Processing auto-monitor message for resource {monitorId} with sku: {monitorResource.SkuName}.");

            var monitorLocation = monitorResource.Location
                    .Replace(" ", string.Empty).ToLowerInvariant();

            await UpdateLogRulesAsync(tagRules, monitorId, partnerEntityId, monitorResource.MonitoringStatus, tenantId, monitorLocation);

            // Continue auto-monitoring resource
            return ProvisioningState.Succeeded;
        }

        public async Task ProcessDeleteMessageAsync(string partnerEntityId, string tenantId)
        {
            if (partnerEntityId == null)
            {
                throw new ArgumentNullException(nameof(partnerEntityId));
            }

            if (tenantId == null)
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var entity = await _partnerDataSource.GetAsync(partnerEntityId);

            if (entity == null)
            {
                // If the entity is null here, then the resource did not get properly created or was already deleted
                _logger.Warning($"Cannot process delete message as entity with id {partnerEntityId} has been deleted or did not get properly created.");
                return;
            }

            var monitorId = entity.ResourceId;

            _logger.Information(
                "Resource {monitorId} has been deleted. Stopping monitoring resources.", monitorId);

            var monitoredResources = await _resourceManager.ListMonitoredResourcesAsync(partnerEntityId, tenantId);
            var trackedResources = await _resourceManager.ListTrackedResourcesAsync(partnerEntityId, tenantId);

            var updates = GetTagRulesUpdateForDeleteRequest(monitoredResources, trackedResources);

            // Stop monitoring all the monitored resources
            await QueueUpdatesAsync(updates, monitorId, partnerEntityId, tenantId);

            // Stop monitoring the subscription, if needed
            await _resourceManager.StopMonitoringSubscriptionAsync(monitorId, partnerEntityId, tenantId);

            // Delete the partner entity from the database
            await _partnerDataSource.DeleteAsync(partnerEntityId);
        }

        private async Task UpdateLogRulesAsync(
            MonitoringTagRules tagRules, string monitorId, string partnerEntityId, MonitoringStatus monitoringStatus, string tenantId, string monitorLocation = null)
        {
            var parsedMonitorId = new ResourceId(monitorId);

            IEnumerable<MonitoredResource> resourcesForLogRules = new List<MonitoredResource>();

            if (tagRules.Properties.LogRules.SendActivityLogs && monitoringStatus == MonitoringStatus.Enabled)
            {
                _logger.Information(
                    "Monitor {@monitorId} with entity {partnerEntityId} is marked for sending logs. Searching for resources to be monitored.",
                    monitorId,
                    partnerEntityId);

                // Filter resources using resource graph only if activity logs flag is enabled
                resourcesForLogRules = await _whaleClient.ListResourcesByTagsAsync(
                    parsedMonitorId.SubscriptionId, tenantId, tagRules.Properties.LogRules.FilteringTags);
            }
            else
            {
                _logger.Information(
                    "Monitor {@monitorId} with entity {partnerEntityId} is marked for not sending logs. Skipping search for resources to be monitored.",
                    monitorId,
                    partnerEntityId);
            }

            var existingMonitoredResources = await _resourceManager.ListMonitoredResourcesAsync(partnerEntityId, tenantId);
            var existingTrackedResources = await _resourceManager.ListTrackedResourcesAsync(partnerEntityId, tenantId);

            var updates = GetTagRulesUpdateForUpdateRequest(resourcesForLogRules, existingMonitoredResources, existingTrackedResources);

            _logger.Information(
                "Queueing updates {@updates} for monitor {monitorId} with entity {partnerEntityId}.",
                updates,
                monitorId,
                partnerEntityId);

            await QueueUpdatesAsync(updates, monitorId, partnerEntityId, tenantId, monitorLocation);
        }

        private async Task QueueUpdatesAsync(MonitoringStateUpdates updates, string monitorId, string partnerEntityId, string tenantId, string monitorLocation = null)
        {
            var tasksForStartMonitoring = updates.ResourcesToStartMonitoring
                .Select(async (resource) =>
                {
                    try
                    {
                        await _resourceManager.StartMonitoringResourceAsync(resource, monitorId, partnerEntityId, tenantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            "An error occurred while starting monitoring resource {resourceId} for monitor {monitorId}. Exception {ex}",
                            resource.Id,
                            monitorId,
                            ex.ToJson());
                    }
                })
                .ToList();

            var tasksForStopMonitoring = updates.ResourcesToStopMonitoring
                .Select(async (resource) =>
                {
                    try
                    {
                        await _resourceManager.StopMonitoringResourceAsync(resource, monitorId, partnerEntityId, tenantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            "An error occurred while stopping monitoring resource {resourceId} for monitor {monitorId}. Exception {ex}",
                            resource.Id,
                            monitorId,
                            ex.ToJson());
                    }
                })
                .ToList();

            var tasksForStopTracking = updates.ResourcesToStopTracking
                .Select(async (resource) =>
                {
                    try
                    {
                        await _resourceManager.StopTrackingResourceAsync(resource.Id, monitorId, partnerEntityId, tenantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            "An error occurred while stopping tracking resource {resourceId} for monitor {monitorId}. Exception {ex}",
                            resource.Id,
                            monitorId,
                            ex.ToJson());
                    }
                })
                .ToList();

            await Task.WhenAll(tasksForStartMonitoring);
            await Task.WhenAll(tasksForStopMonitoring);
            await Task.WhenAll(tasksForStopTracking);
        }

        private static MonitoringStateUpdates GetTagRulesUpdateForUpdateRequest(
            IEnumerable<MonitoredResource> resourcesFromResourceGraph,
            IEnumerable<MonitoredResource> resourcesBeingMonitored,
            IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus> resourcesBeingTracked)
        {
            var resourceGraphHashSet = new HashSet<MonitoredResource>(
                resourcesFromResourceGraph, new MonitoredResourceComparer());

            var monitoredHashSet = new HashSet<MonitoredResource>(
                resourcesBeingMonitored, new MonitoredResourceComparer());

            List<MonitoredResource> resourcesToStartMonitoring = GetResourcesToStartMonitoring(resourcesFromResourceGraph, resourcesBeingTracked, monitoredHashSet);
            List<MonitoredResource> resourcesToStopMonitoring = GetResourcesToStopMonitoring(resourcesBeingMonitored, resourcesBeingTracked, resourceGraphHashSet);
            List<MonitoredResource> resourcesToStopTracking = GetResourcesToStopTracking(resourcesBeingTracked, resourceGraphHashSet);

            var monitoringStateUpdates = new MonitoringStateUpdates()
            {
                ResourcesToStartMonitoring = resourcesToStartMonitoring,
                ResourcesToStopMonitoring = resourcesToStopMonitoring,
                ResourcesToStopTracking = resourcesToStopTracking,
            };

            return monitoringStateUpdates;
        }

        private static MonitoringStateUpdates GetTagRulesUpdateForDeleteRequest(
            IEnumerable<MonitoredResource> resourcesBeingMonitored,
            IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus> resourcesBeingTracked)
        {
            var resourcesToStopTracking = resourcesBeingTracked
                .Where(m => m.IsMonitored == false)
                .Select(m => new MonitoredResource() { Id = m.MonitoredResourceId });

            var monitoringStateUpdates = new MonitoringStateUpdates()
            {
                ResourcesToStartMonitoring = new List<MonitoredResource>(),
                ResourcesToStopMonitoring = resourcesBeingMonitored,
                ResourcesToStopTracking = resourcesToStopTracking,
            };

            return monitoringStateUpdates;
        }

        private static List<MonitoredResource> GetResourcesToStopTracking(IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus> resourcesBeingTracked, HashSet<MonitoredResource> resourceGraphHashSet)
        {
            var nonMonitoredTaggedResources = resourcesBeingTracked
                            .Where(m => m.IsMonitored == false && m.Reason != MonitoringStatusReason.CreatedByUser.GetReasonName())
                            .Select(m => new MonitoredResource() { Id = m.MonitoredResourceId });

            // Stop tracking resources not captured by the filter rules
            var resourcesToStopTracking = nonMonitoredTaggedResources.Where(r => !resourceGraphHashSet.Contains(r)).ToList();
            return resourcesToStopTracking;
        }

        private static List<MonitoredResource> GetResourcesToStopMonitoring(IEnumerable<MonitoredResource> resourcesBeingMonitored, IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus> resourcesBeingTracked, HashSet<MonitoredResource> resourceGraphHashSet)
        {
            var userTrackedResources = resourcesBeingTracked
                            .Where(m => m.Reason == MonitoringStatusReason.CreatedByUser.GetReasonName())
                            .Select(m => new MonitoredResource() { Id = m.MonitoredResourceId });
            var userTrackedResourcesHashSet = new HashSet<MonitoredResource>(
               userTrackedResources, new MonitoredResourceComparer());
            // Stop monitoring resources not captured by the filter rules
            var resourcesToStopMonitoring = resourcesBeingMonitored
            .Where(r => !resourceGraphHashSet.Contains(r))
            .Where(r => !userTrackedResourcesHashSet.Contains(r))
            .ToList();
            return resourcesToStopMonitoring;
        }

        private static List<MonitoredResource> GetResourcesToStartMonitoring(IEnumerable<MonitoredResource> resourcesFromResourceGraph, IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus> resourcesBeingTracked, HashSet<MonitoredResource> monitoredHashSet)
        {
            // Start monitoring tagged resources not being monitored
            var taggedResourcesToStartMonitoring = resourcesFromResourceGraph.Where(r => !monitoredHashSet.Contains(r)).ToList();

            // Start monitoring tracked user-resources not being monitored
            var userResourcesToStartMonitoring = resourcesBeingTracked
                .Where(m => m.IsMonitored == false && m.Reason == MonitoringStatusReason.CreatedByUser.GetReasonName())
                .Select(m => new MonitoredResource() { Id = m.MonitoredResourceId });

            var resourcesToStartMonitoring = taggedResourcesToStartMonitoring.Union(userResourcesToStartMonitoring);
            return resourcesToStartMonitoring.ToList();
        }
    }
}
