//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.EventHubManager;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Whale
{
    public class MonitoredResourceManager : IMonitoredResourceManager
    {
        private readonly IAzureClientsProvider _clientProvider;
        private readonly IMonitoringRelationshipDataSource<MonitoringRelationship> _relationshipDataSource;
        private readonly IMonitoringStatusDataSource<DataSource.Mongo.MonitoringSvc.MonitoringStatus> _statusDataSource;
        private readonly IEventHubManager _eventHubManager;
        private readonly ILogger _logger;

        public MonitoredResourceManager(
            IAzureClientsProvider clientProvider,
            IMonitoringRelationshipDataSource<MonitoringRelationship> relationshipDataSource,
            IMonitoringStatusDataSource<DataSource.Mongo.MonitoringSvc.MonitoringStatus> statusDataSource,
            IEventHubManager eventHubManager,
            ILogger logger)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
            _relationshipDataSource = relationshipDataSource ?? throw new ArgumentNullException(nameof(relationshipDataSource));
            _statusDataSource = statusDataSource ?? throw new ArgumentNullException(nameof(statusDataSource));
            _eventHubManager = eventHubManager ?? throw new ArgumentNullException(nameof(eventHubManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start monitoring a resource, by adding diagnostic settings to it if needed.
        /// </summary>
        public async Task StartMonitoringResourceAsync(MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            using var operation = _logger.StartTimedOperation(nameof(StartMonitoringResourceAsync));
            var diagnosticSettingManager = new DiagnosticSettingsManager(_clientProvider, _eventHubManager, _logger);
            MonitoringRelationship relationshipEntity = new MonitoringRelationship()
            {
                PartnerEntityId = partnerEntityId,
                MonitoredResourceId = resource.Id.ToUpperInvariant(),
                TenantId = tenantId,
            };

            MonitoringStatus statusEntity = new MonitoringStatus()
            {
                PartnerEntityId = partnerEntityId,
                MonitoredResourceId = resource.Id.ToUpperInvariant(),
                TenantId = tenantId,
            };

            try
            {
                operation.SetContextProperty("monitoredResourceId", resource.Id);
                operation.SetContextProperty("monitoredResourceLocation", resource.Location);
                operation.SetContextProperty(nameof(monitorId), monitorId);
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);

                var existingMonitoringRelationships = await GetExistingRelationshipEntitiesAsync(resource, tenantId);
                var resourceAlreadyMonitored = existingMonitoringRelationships.Any();
                operation.SetContextProperty(nameof(resourceAlreadyMonitored), resourceAlreadyMonitored.ToString(CultureInfo.InvariantCulture));

                if (resourceAlreadyMonitored)
                {
                    // Resource is already being monitored by other Datadog resource. We can skip
                    // the diagnostic settings logic and just add a new entity to the database.
                    statusEntity.IsMonitored = true;
                    statusEntity.Reason = MonitoringStatusReason.CapturedByRules.GetReasonName();
                    relationshipEntity.EventhubName = existingMonitoringRelationships.First().EventhubName;
                    relationshipEntity.AuthorizationRuleId = existingMonitoringRelationships.First().AuthorizationRuleId;
                    relationshipEntity.DiagnosticSettingsName = existingMonitoringRelationships.First().DiagnosticSettingsName;

                    _logger.Information(
                        "Resource {resourceId} is already being monitored by other Datadog resource. Reusing diagnostic setting {diagnosticSettingName}.",
                        resource.Id,
                        relationshipEntity.DiagnosticSettingsName);
                }
                else
                {
                    // Resource is not being monitored yet. We need to add a diagnostic
                    // setting to it before adding a new entity to the database.
                    _logger.Information(
                        "Resource {resourceId} is not being monitored yet. Attempting to start monitoring it.", resource.Id);

                    await diagnosticSettingManager.AddDiagnosticSettingToResourceAsync(resource, monitorId, tenantId);

                    statusEntity.IsMonitored = diagnosticSettingManager.SuccessfulOperation;
                    statusEntity.Reason = diagnosticSettingManager.Reason.GetReasonName();

                    _logger.Information(
                        "Operation to add diagnostic setting to resource {resourceId} has status {isMonitored} and reason {reason}.",
                        resource.Id,
                        statusEntity.IsMonitored,
                        statusEntity.Reason);

                    relationshipEntity.EventhubName = diagnosticSettingManager.EventHubName;
                    relationshipEntity.AuthorizationRuleId = diagnosticSettingManager.AuthorizationRuleId;
                    relationshipEntity.DiagnosticSettingsName = diagnosticSettingManager.DiagnosticSettingName;
                }

                operation.SetContextProperty(nameof(statusEntity.IsMonitored), statusEntity.IsMonitored.ToString(CultureInfo.InvariantCulture));
                operation.SetContextProperty(nameof(statusEntity.Reason), statusEntity.Reason);
                operation.SetContextProperty(nameof(relationshipEntity.EventhubName), relationshipEntity.EventhubName);
                operation.SetContextProperty(nameof(relationshipEntity.AuthorizationRuleId), relationshipEntity.AuthorizationRuleId);
                operation.SetContextProperty(nameof(relationshipEntity.DiagnosticSettingsName), relationshipEntity.DiagnosticSettingsName);

                await _statusDataSource.AddOrUpdateAsync(statusEntity);

                if (statusEntity.IsMonitored)
                {
                    _logger.Information(
                        "Adding new entity for resource {resourceId} and Datadog monitor {monitorId}.",
                        resource.Id,
                        monitorId);

                    await _relationshipDataSource.AddAsync(relationshipEntity);

                    operation.SetResultDescription("Started monitoring resource successfully.");
                }
                else
                {
                    operation.SetResultDescription("Could not start monitoring resource.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Failed to start monitoring resource {resourceId} for monitor {monitorId}.",
                    resource.Id,
                    monitorId);

                if (ex is ErrorResponseException)
                {
                    var castedEx = ex as ErrorResponseException;
                    _logger.Error("Error code: {errCode}, Error message: '{errMessage}'", castedEx.Body.Code, castedEx.Body.Message);
                }

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Stop monitoring a resource, removing the diagnostic setting from it if needed.
        /// </summary>
        public async Task StopMonitoringResourceAsync(MonitoredResource resource, string monitorId, string partnerEntityId, string tenantId)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            using var operation = _logger.StartTimedOperation(nameof(StopMonitoringResourceAsync));
            var diagnosticSettingManager = new DiagnosticSettingsManager(_clientProvider, _eventHubManager, _logger);

            try
            {
                operation.SetContextProperty("monitoredResourceId", resource.Id);
                operation.SetContextProperty(nameof(monitorId), monitorId);
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);

                var existingRelationshipEntities = await GetExistingRelationshipEntitiesAsync(resource, tenantId);
                var shouldRemoveDiagnosticSetting = existingRelationshipEntities.Count() == 1;

                operation.SetContextProperty(nameof(shouldRemoveDiagnosticSetting), shouldRemoveDiagnosticSetting.ToString(CultureInfo.InvariantCulture));

                if (shouldRemoveDiagnosticSetting)
                {
                    var diagnosticSettingName = existingRelationshipEntities.First().DiagnosticSettingsName;

                    operation.SetContextProperty(nameof(diagnosticSettingName), diagnosticSettingName);
                    _logger.Information(
                        "Deleting diagnostic setting {diagnosticSettingName} from resource {resourceId}.",
                        diagnosticSettingName,
                        resource.Id);

                    await diagnosticSettingManager.RemoveDiagnosticSettingFromResourceAsync(resource, diagnosticSettingName, monitorId, tenantId);
                }

                if (diagnosticSettingManager.SuccessfulOperation)
                {
                    _logger.Information(
                        "Deleting database entity for resource {resourceId} and monitor {monitorId}.",
                        resource.Id,
                        monitorId);

                    await _statusDataSource.DeleteAsync(tenantId, partnerEntityId, resource.Id.ToUpperInvariant());
                    await _relationshipDataSource.DeleteAsync(tenantId, partnerEntityId, resource.Id.ToUpperInvariant());
                }
                else
                {
                    throw new InvalidOperationException("Failed to remove the diagnostic setting from the resource.");
                }

                operation.SetResultDescription("Stopped monitoring resource successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to stop monitoring resource {resourceId} for monitor {monitorId}. Exception {ex}",
                    resource.Id,
                    monitorId,
                    ex.Message);

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Start monitoring a subscription, adding a diagnostic setting to it if needed.
        /// </summary>
        public async Task StartMonitoringSubscriptionAsync(string monitorId, string location, string partnerEntityId, string tenantId)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException(nameof(location));
            }

            var parsedMonitorId = new ResourceId(monitorId);
            var subscriptionId = parsedMonitorId.SubscriptionId;
            var subscriptionResourceId = "/subscriptions/" + subscriptionId;

            using var operation = _logger.StartTimedOperation(nameof(StartMonitoringSubscriptionAsync));

            try
            {
                operation.SetContextProperty(nameof(monitorId), monitorId);
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);

                var monitorLocation = location
                    .Replace(" ", string.Empty).ToLowerInvariant();

                var subscriptionMonitoredResource = new MonitoredResource()
                {
                    Id = subscriptionResourceId,
                    Location = monitorLocation,
                };

                operation.SetContextProperty(nameof(subscriptionResourceId), subscriptionResourceId);
                operation.SetContextProperty(nameof(monitorLocation), monitorLocation);

                var existingMonitoringRelationships = await GetExistingRelationshipEntitiesAsync(subscriptionMonitoredResource, tenantId);
                var hasDiagnosticSetting = existingMonitoringRelationships.Any(e => e.PartnerEntityId.OrdinalEquals(partnerEntityId));
                operation.SetContextProperty(nameof(hasDiagnosticSetting), hasDiagnosticSetting.ToString(CultureInfo.InvariantCulture));

                if (hasDiagnosticSetting)
                {
                    _logger.Information("Subscription {subscriptionId} is already sending logs; nothing to update.", subscriptionId);
                    operation.SetResultDescription("Subscription is already being monitored, nothing to do.");
                }
                else
                {
                    var diagnosticSettingManager = new DiagnosticSettingsManager(_clientProvider, _eventHubManager, _logger);
                    MonitoringRelationship relationshipEntity = new MonitoringRelationship()
                    {
                        PartnerEntityId = partnerEntityId,
                        MonitoredResourceId = subscriptionMonitoredResource.Id.ToUpperInvariant(),
                        TenantId = tenantId,
                    };

                    DataSource.Mongo.MonitoringSvc.MonitoringStatus statusEntity = new DataSource.Mongo.MonitoringSvc.MonitoringStatus()
                    {
                        PartnerEntityId = partnerEntityId,
                        MonitoredResourceId = subscriptionMonitoredResource.Id.ToUpperInvariant(),
                        TenantId = tenantId,
                    };

                    if (existingMonitoringRelationships.Any())
                    {
                        statusEntity.IsMonitored = true;
                        statusEntity.Reason = MonitoringStatusReason.CapturedByRules.GetReasonName();
                        relationshipEntity.EventhubName = existingMonitoringRelationships.First().EventhubName;
                        relationshipEntity.AuthorizationRuleId = existingMonitoringRelationships.First().AuthorizationRuleId;
                        relationshipEntity.DiagnosticSettingsName = existingMonitoringRelationships.First().DiagnosticSettingsName;

                        _logger.Information(
                            "Subscription {subscriptionId} is already being monitored by other Datadog resource. Reusing diagnostic setting {diagnosticSettingName}.",
                            subscriptionId,
                            relationshipEntity.DiagnosticSettingsName);
                    }
                    else
                    {
                        // subscription is not being monitored yet. We need to add a diagnostic
                        // setting to it before adding a new entity to the database.
                        _logger.Information(
                            "subscription {subscriptionId} is not being monitored yet. Attempting to start monitoring it.", subscriptionMonitoredResource.Id);

                        await diagnosticSettingManager.AddDiagnosticSettingToSubscriptionAsync(subscriptionMonitoredResource, subscriptionId, monitorId, tenantId);

                        statusEntity.IsMonitored = diagnosticSettingManager.SuccessfulOperation;
                        statusEntity.Reason = diagnosticSettingManager.Reason.GetReasonName();

                        _logger.Information(
                            "Operation to add diagnostic setting to subscription {subscriptionId} has status {isMonitored} and reason {reason}.",
                            subscriptionMonitoredResource.Id,
                            statusEntity.IsMonitored,
                            statusEntity.Reason);

                        relationshipEntity.EventhubName = diagnosticSettingManager.EventHubName;
                        relationshipEntity.AuthorizationRuleId = diagnosticSettingManager.AuthorizationRuleId;
                        relationshipEntity.DiagnosticSettingsName = diagnosticSettingManager.DiagnosticSettingName;
                    }

                    operation.SetContextProperty(nameof(statusEntity.IsMonitored), statusEntity.IsMonitored.ToString(CultureInfo.InvariantCulture));
                    operation.SetContextProperty(nameof(statusEntity.Reason), statusEntity.Reason);
                    operation.SetContextProperty(nameof(relationshipEntity.EventhubName), relationshipEntity.EventhubName);
                    operation.SetContextProperty(nameof(relationshipEntity.AuthorizationRuleId), relationshipEntity.AuthorizationRuleId);
                    operation.SetContextProperty(nameof(relationshipEntity.DiagnosticSettingsName), relationshipEntity.DiagnosticSettingsName);

                    await _statusDataSource.AddOrUpdateAsync(statusEntity);

                    if (diagnosticSettingManager.SuccessfulOperation)
                    {
                        _logger.Information(
                            "Adding new entity for subscription {subscriptionId} and Datadog monitor {monitorId}.",
                            subscriptionMonitoredResource.Id,
                            monitorId);

                        await _relationshipDataSource.AddAsync(relationshipEntity);

                        operation.SetResultDescription("Started monitoring subscription successfully.");
                    }
                    else
                    {
                        operation.SetResultDescription("Could not start monitoring subscription.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Failed to start monitoring subscription {subscriptionId} for monitor {monitorId}.",
                    subscriptionResourceId,
                    monitorId);

                if (ex is ErrorResponseException)
                {
                    var castedEx = ex as ErrorResponseException;
                    _logger.Error("Error code: {errCode}, Error message: '{errMessage}'", castedEx.Body.Code, castedEx.Body.Message);
                }

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Stop monitoring a subscription, removing the diagnostic setting to it if needed.
        /// </summary>
        public async Task StopMonitoringSubscriptionAsync(string monitorId, string partnerEntityId, string tenantId)
        {
            var parsedMonitorId = new ResourceId(monitorId);
            var subscriptionId = parsedMonitorId.SubscriptionId;
            var subscriptionResourceId = "/subscriptions/" + subscriptionId;

            using var operation = _logger.StartTimedOperation(nameof(StopMonitoringSubscriptionAsync));

            try
            {
                operation.SetContextProperty(nameof(monitorId), monitorId);
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);
                operation.SetContextProperty(nameof(subscriptionResourceId), subscriptionResourceId);

                var subscriptionMonitoredResource = new MonitoredResource()
                {
                    Id = subscriptionResourceId,
                    Location = string.Empty,
                };

                var existingMonitoringRelationships = await GetExistingRelationshipEntitiesAsync(subscriptionMonitoredResource, tenantId);
                var hasDiagnosticSetting = existingMonitoringRelationships.Any(e => e.PartnerEntityId.OrdinalEquals(partnerEntityId));
                operation.SetContextProperty(nameof(hasDiagnosticSetting), hasDiagnosticSetting.ToString(CultureInfo.InvariantCulture));

                if (hasDiagnosticSetting)
                {
                    // Is sending logs, and should stop sending logs.
                    _logger.Information("Subscription {subscriptionId} is sending logs; stopping monitoring it.", subscriptionId);

                    var shouldRemoveDiagnosticSetting = existingMonitoringRelationships.Count() == 1;
                    var diagnosticSettingManager = new DiagnosticSettingsManager(_clientProvider, _eventHubManager, _logger);

                    operation.SetContextProperty(nameof(shouldRemoveDiagnosticSetting), shouldRemoveDiagnosticSetting.ToString(CultureInfo.InvariantCulture));

                    if (shouldRemoveDiagnosticSetting)
                    {
                        var diagnosticSettingName = existingMonitoringRelationships.First().DiagnosticSettingsName;

                        operation.SetContextProperty(nameof(diagnosticSettingName), diagnosticSettingName);
                        _logger.Information(
                            "Deleting diagnostic setting {diagnosticSettingName} from subscription {subscriptionId}",
                            diagnosticSettingName,
                            subscriptionId);

                        await diagnosticSettingManager.RemoveDiagnosticSettingFromSubscriptionAsync(subscriptionId, diagnosticSettingName, monitorId, tenantId);
                    }

                    if (diagnosticSettingManager.SuccessfulOperation)
                    {
                        _logger.Information(
                            "Deleting database entity for subscription {subscriptionId} and monitor {monitorId}.",
                            subscriptionResourceId,
                            monitorId);

                        await _statusDataSource.DeleteAsync(tenantId, partnerEntityId, subscriptionResourceId.ToUpperInvariant());
                        await _relationshipDataSource.DeleteAsync(tenantId, partnerEntityId, subscriptionResourceId.ToUpperInvariant());
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to remove the diagnostic setting from the subscription.");
                    }

                    operation.SetResultDescription("Stopped monitoring subscription successfully.");
                }
                else
                {
                    // Is not sending logs, and should continue not sending logs.
                    _logger.Information("Subscription {subscriptionId} is already not sending logs; nothing to update.", subscriptionId);
                    operation.SetResultDescription("Subscription is already not being monitored, nothing to do.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to stop monitoring subscription {subscriptionId} for monitor {monitorId}. Exception {ex}",
                    subscriptionResourceId,
                    monitorId,
                    ex.Message);

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// List the resources currently being monitored.
        /// </summary>
        public async Task<IEnumerable<MonitoredResource>> ListMonitoredResourcesAsync(string partnerEntityId, string tenantId)
        {
            using var operation = _logger.StartTimedOperation(nameof(ListMonitoredResourcesAsync));

            try
            {
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);
                operation.SetContextProperty(nameof(tenantId), tenantId);

                var databaseEntities = await _relationshipDataSource.ListByPartnerResourceAsync(tenantId, partnerEntityId);

                var monitoredResourcesList = databaseEntities
                    .Where(e =>
                    {
                        // We need to exclude the entities associated to a subscription instead
                        // of a resource, as subscription logs are handled separately. We do it
                        // by checking that the number of non-empty segments in the resource id
                        // is greater than 3, or larger than "/subscriptions/{subscriptionId}".
                        var parsedId = RemoveEmptyEntries(e.MonitoredResourceId.Split('/'));
                        return parsedId.Length > 2;
                    })
                    .Select(e => new MonitoredResource() { Id = e.MonitoredResourceId });

                _logger.Information("Listed {n} monitored resources for partnerEntityId: {partnerEntityId}", monitoredResourcesList.Count(), partnerEntityId);
                operation.SetResultDescription("Listed monitored resources successfully.");

                return monitoredResourcesList.ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to list monitored resources for partner entity {partnerEntityId}. Exception {ex}",
                    partnerEntityId,
                    ex.Message);

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// List the resources tracked for monitoring, regardless of active status of the resource.
        /// </summary>
        public async Task<IEnumerable<DataSource.Mongo.MonitoringSvc.MonitoringStatus>> ListTrackedResourcesAsync(string partnerEntityId, string tenantId)
        {
            using var operation = _logger.StartTimedOperation(nameof(ListTrackedResourcesAsync));

            try
            {
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);
                operation.SetContextProperty(nameof(tenantId), tenantId);

                var databaseEntities = await _statusDataSource.ListByPartnerResourceAsync(tenantId, partnerEntityId);

                var resourcesList = databaseEntities
                    .Where(e =>
                    {
                        // We need to exclude the entities associated to a subscription instead
                        // of a resource, as subscription logs are handled separately. We do it
                        // by checking that the number of non-empty segments in the resource id
                        // is greater than 3, or larger than "/subscriptions/{subscriptionId}".
                        var parsedId = RemoveEmptyEntries(e.MonitoredResourceId.Split('/'));
                        return parsedId.Length > 2;
                    });

                _logger.Information("Listed {n} tracked resources for partnerEntityId: {partnerEntityId}", resourcesList.Count(), partnerEntityId);
                operation.SetResultDescription("Listed tracked resources successfully.");

                return resourcesList.ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to list monitored resources for partner entity {partnerEntityId}. Exception {ex}",
                    partnerEntityId,
                    ex.Message);

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Stop tracking a non-monitored resource, by removing it from the database.
        /// </summary>
        public async Task StopTrackingResourceAsync(string resourceId, string monitorId, string partnerEntityId, string tenantId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            using var operation = _logger.StartTimedOperation(nameof(StopTrackingResourceAsync));

            try
            {
                operation.SetContextProperty(nameof(resourceId), resourceId);
                operation.SetContextProperty(nameof(monitorId), monitorId);
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);
                operation.SetContextProperty(nameof(tenantId), tenantId);

                await _statusDataSource.DeleteAsync(tenantId, partnerEntityId, resourceId.ToUpperInvariant());

                operation.SetResultDescription("Stopped tracking resource successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to stop tracking resource {resourceId} for partner entity {partnerEntityId}. Exception {ex}",
                    resourceId,
                    partnerEntityId,
                    ex.Message);

                operation.FailOperation(ex.Message);

                throw;
            }
        }

        #region Database utilities

        private async Task<IEnumerable<IMonitoringRelationship>> GetExistingRelationshipEntitiesAsync(MonitoredResource resource, string tenantId)
        {
            var entities = await _relationshipDataSource.ListByMonitoredResourceAsync(tenantId, resource.Id.ToUpperInvariant());
            return entities.ToList();
        }

        #endregion

        private string[] RemoveEmptyEntries(string[] entries)
        {
            List<string> newEntries = new List<string>();
            foreach (string entry in entries)
            {
                if (!string.IsNullOrEmpty(entry))
                {
                    newEntries.Add(entry);
                }
            }

            return newEntries.ToArray();
        }
    }
}
