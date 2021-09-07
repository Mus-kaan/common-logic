//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Whale
{
    public class MonitoredResourceManager : IMonitoredResourceManager
    {
        private readonly IAzureClientsProvider _clientProvider;
        private readonly IMonitoringRelationshipDataSource<MonitoringRelationship> _relationshipDataSource;
        private readonly IMonitoringStatusDataSource<DataSource.Mongo.MonitoringSvc.MonitoringStatus> _statusDataSource;
        private readonly IDiagnosticSettingsManager _diagnosticSettingsManager;
        private readonly ILogger _logger;

        public MonitoredResourceManager(
            IAzureClientsProvider clientProvider,
            IMonitoringRelationshipDataSource<MonitoringRelationship> relationshipDataSource,
            IMonitoringStatusDataSource<DataSource.Mongo.MonitoringSvc.MonitoringStatus> statusDataSource,
            IDiagnosticSettingsManager diagnosticSettingsManager,
            ILogger logger)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
            _relationshipDataSource = relationshipDataSource ?? throw new ArgumentNullException(nameof(relationshipDataSource));
            _statusDataSource = statusDataSource ?? throw new ArgumentNullException(nameof(statusDataSource));
            _diagnosticSettingsManager = diagnosticSettingsManager ?? throw new ArgumentNullException(nameof(diagnosticSettingsManager));
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

            using var operation = _logger.StartTimedOperation(nameof(StartMonitoringResourceAsync), newCorrelationId: true);
            MonitoringRelationship relationshipEntity = new MonitoringRelationship()
            {
                PartnerEntityId = partnerEntityId,
                MonitoredResourceId = resource.Id.ToUpperInvariant(),
                TenantId = tenantId,
            };

            DataSource.Mongo.MonitoringSvc.MonitoringStatus statusEntity = new DataSource.Mongo.MonitoringSvc.MonitoringStatus()
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

                _logger.Information("Attempting to start monitoring resource: {resourceId}.", resource.Id);
                var dsManagerGetResponse = await _diagnosticSettingsManager.ListResourceDiagnosticSettingsAsync(resource.Id, tenantId);

                var successfulDSAddOperation = true;
                string diagnosticSettingsName = null;
                MonitoringStatusReason reason = MonitoringStatusReason.CapturedByRules;

                if (dsManagerGetResponse.SuccessfulOperation)
                {
                    if (dsManagerGetResponse.DiagnosticSettingV2ModelList.Count >= 5)
                    {
                        _logger.Information(
                        "Resource {resourceId} can't be monitored as it already has 5 diagnostic settings.",
                        resource.Id);

                        successfulDSAddOperation = false;
                        reason = MonitoringStatusReason.DiagnosticSettingsLimitReached;
                    }
                    else
                    {
                        var dsManagerAddResponse = await _diagnosticSettingsManager.CreateOrUpdateResourceDiagnosticSettingAsync(resource.Id, monitorId, tenantId);
                        successfulDSAddOperation = dsManagerAddResponse.SuccessfulOperation;
                        reason = successfulDSAddOperation ? MonitoringStatusReason.CapturedByRules : MonitoringStatusReason.Other;
                        diagnosticSettingsName = dsManagerAddResponse.DiagnosticSettingsName;
                    }
                }
                else
                {
                    successfulDSAddOperation = false;
                    reason = MonitoringStatusReason.Other;
                }

                statusEntity.IsMonitored = successfulDSAddOperation;
                statusEntity.Reason = reason.GetReasonName();

                _logger.Information(
                    "Operation to add diagnostic setting to resource {resourceId} has status {isMonitored} and reason {reason}.",
                    resource.Id,
                    statusEntity.IsMonitored,
                    statusEntity.Reason);

                relationshipEntity.DiagnosticSettingsName = diagnosticSettingsName;
                operation.SetContextProperty(nameof(statusEntity.IsMonitored), statusEntity.IsMonitored.ToString(CultureInfo.InvariantCulture));
                operation.SetContextProperty(nameof(statusEntity.Reason), statusEntity.Reason);
                operation.SetContextProperty(nameof(relationshipEntity.DiagnosticSettingsName), relationshipEntity.DiagnosticSettingsName);

                await _statusDataSource.AddOrUpdateAsync(statusEntity);

                if (successfulDSAddOperation)
                {
                    _logger.Information(
                        "Adding new entity for resource {resourceId} and monitor {monitorId}.",
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

            using var operation = _logger.StartTimedOperation(nameof(StopMonitoringResourceAsync), newCorrelationId: true);
            try
            {
                operation.SetContextProperty("monitoredResourceId", resource.Id);
                operation.SetContextProperty(nameof(monitorId), monitorId);
                operation.SetContextProperty(nameof(partnerEntityId), partnerEntityId);

                var existingRelationshipEntities = await GetExistingRelationshipEntitiesAsync(resource, partnerEntityId, tenantId);
                IDiagnosticSettingsManagerResult dsManagerResponse = null;
                var diagnosticSettingName = existingRelationshipEntities.First().DiagnosticSettingsName;
                operation.SetContextProperty(nameof(diagnosticSettingName), diagnosticSettingName);
                _logger.Information(
                    "Deleting diagnostic setting {diagnosticSettingName} from resource {resourceId}.",
                    diagnosticSettingName,
                    resource.Id);

                dsManagerResponse = await _diagnosticSettingsManager.RemoveResourceDiagnosticSettingAsync(resource.Id, diagnosticSettingName, tenantId);

                if (dsManagerResponse.SuccessfulOperation)
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

                var monitorLocation = location.Replace(" ", string.Empty).ToLowerInvariant();

                var subscriptionMonitoredResource = new MonitoredResource()
                {
                    Id = subscriptionResourceId,
                    Location = monitorLocation,
                };

                operation.SetContextProperty(nameof(subscriptionResourceId), subscriptionResourceId);
                operation.SetContextProperty(nameof(monitorLocation), monitorLocation);

                var existingMonitoringRelationships = await GetExistingRelationshipEntitiesAsync(subscriptionResourceId, tenantId);
                var hasDiagnosticSetting = existingMonitoringRelationships.Any(e => e.PartnerEntityId.OrdinalEquals(partnerEntityId));
                operation.SetContextProperty(nameof(hasDiagnosticSetting), hasDiagnosticSetting.ToString(CultureInfo.InvariantCulture));

                if (hasDiagnosticSetting)
                {
                    _logger.Information("Subscription {subscriptionId} is already sending logs; nothing to update.", subscriptionId);
                    operation.SetResultDescription("Subscription is already being monitored, nothing to do.");
                }
                else
                {
                    _logger.Information(
                            "subscription {subscriptionId} is not being monitored yet. Attempting to start monitoring it.", subscriptionMonitoredResource.Id);

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

                    var dsAddResult = await _diagnosticSettingsManager.CreateOrUpdateSubscriptionDiagnosticSettingAsync(subscriptionResourceId, monitorId, tenantId);

                    statusEntity.IsMonitored = dsAddResult.SuccessfulOperation;
                    statusEntity.Reason = dsAddResult.Reason.GetReasonName();

                    _logger.Information(
                        "Operation to add diagnostic setting to subscription {subscriptionId} has status {isMonitored} and reason {reason}.",
                        subscriptionMonitoredResource.Id,
                        statusEntity.IsMonitored,
                        statusEntity.Reason);

                    relationshipEntity.DiagnosticSettingsName = dsAddResult.DiagnosticSettingsName;

                    operation.SetContextProperty(nameof(statusEntity.IsMonitored), statusEntity.IsMonitored.ToString(CultureInfo.InvariantCulture));
                    operation.SetContextProperty(nameof(statusEntity.Reason), statusEntity.Reason);
                    operation.SetContextProperty(nameof(relationshipEntity.DiagnosticSettingsName), relationshipEntity.DiagnosticSettingsName);

                    await _statusDataSource.AddOrUpdateAsync(statusEntity);

                    if (dsAddResult.SuccessfulOperation)
                    {
                        _logger.Information(
                            "Adding new entity for subscription {subscriptionId} and monitor {monitorId}.",
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

                var existingMonitoringRelationships = await GetExistingRelationshipEntitiesAsync(subscriptionResourceId, tenantId);
                var hasDiagnosticSetting = existingMonitoringRelationships.Any(e => e.PartnerEntityId.OrdinalEquals(partnerEntityId));
                operation.SetContextProperty(nameof(hasDiagnosticSetting), hasDiagnosticSetting.ToString(CultureInfo.InvariantCulture));

                if (hasDiagnosticSetting)
                {
                    // Is sending logs, and should stop sending logs.
                    _logger.Information("Subscription {subscriptionId} is sending logs; stopping monitoring it.", subscriptionId);
                    var diagnosticSettingName = existingMonitoringRelationships
                        .Where(e => e.PartnerEntityId.OrdinalEquals(partnerEntityId))
                        .First()
                        .DiagnosticSettingsName;

                    operation.SetContextProperty(nameof(diagnosticSettingName), diagnosticSettingName);
                    _logger.Information(
                        "Deleting diagnostic setting {diagnosticSettingName} from subscription {subscriptionId}",
                        diagnosticSettingName,
                        subscriptionId);

                    var dsManagerRemoveResponse = await _diagnosticSettingsManager.RemoveSubscriptionDiagnosticSettingAsync(subscriptionResourceId, diagnosticSettingName, monitorId, tenantId);

                    if (dsManagerRemoveResponse.SuccessfulOperation)
                    {
                        _logger.Information(
                            "Deleting database entity for subscription {subscriptionId} and monitor {monitorId}.",
                            subscriptionResourceId,
                            monitorId);

                        await _statusDataSource.DeleteAsync(tenantId, partnerEntityId, subscriptionResourceId.ToUpperInvariant());
                        await _relationshipDataSource.DeleteAsync(tenantId, partnerEntityId, subscriptionResourceId.ToUpperInvariant());
                        operation.SetResultDescription("Stopped monitoring subscription successfully.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to remove the diagnostic setting from the subscription.");
                    }
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
                        var parsedId = e.MonitoredResourceId.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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
                        var parsedId = e.MonitoredResourceId.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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

        private async Task<IEnumerable<IMonitoringRelationship>> GetExistingRelationshipEntitiesAsync(MonitoredResource resource, string partnerEntityId, string tenantId)
        {
            var entities = await _relationshipDataSource.ListByMonitoredResourceAsync(tenantId, resource.Id.ToUpperInvariant());
            return entities
            .Where(e => e.PartnerEntityId == partnerEntityId)
            .ToList();
        }

        private async Task<IEnumerable<IMonitoringRelationship>> GetExistingRelationshipEntitiesAsync(string resourceId, string tenantId)
        {
            var entities = await _relationshipDataSource.ListByMonitoredResourceAsync(tenantId, resourceId.ToUpperInvariant());
            return entities.ToList();
        }

        #endregion
    }
}
