//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.Common.Interfaces;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Notifications.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Notifications
{
    public abstract class NotificationManagerBase : INotificationManager
    {
        protected readonly ILogger _logger;
        protected readonly IDiagnosticSettingsManager _diagnosticManager;
        protected readonly IPartnerResourceDataSource<PartnerResourceEntity> _partnerDataSource;
        protected readonly IMonitoringRelationshipDataSource<MonitoringRelationship> _relationshipDataSource;
        protected readonly IMonitoringStatusDataSource<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus> _statusDataSource;
        private readonly IWhaleFilterClient _whaleClient;
        private readonly ISubscriptionVersionSelector _subVersionSelector;
        private readonly DiagnosticSettingsHelper _dsHelper;

        protected NotificationManagerBase(
            IWhaleFilterClient whaleFilterClient,
            ISubscriptionVersionSelector subVersionSelector,
            IDiagnosticSettingsManager diagnosticManager,
            IPartnerResourceDataSource<PartnerResourceEntity> partnerDataSource,
            IMonitoringRelationshipDataSource<MonitoringRelationship> relationshipDataSource,
            IMonitoringStatusDataSource<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus> statusDataSource,
            DiagnosticSettingsHelper dsHelper,
            ILogger logger)
        {
            _whaleClient = whaleFilterClient ?? throw new ArgumentNullException(nameof(whaleFilterClient));
            _subVersionSelector = subVersionSelector ?? throw new ArgumentNullException(nameof(subVersionSelector));
            _diagnosticManager = diagnosticManager ?? throw new ArgumentNullException(nameof(diagnosticManager));
            _partnerDataSource = partnerDataSource ?? throw new ArgumentNullException(nameof(partnerDataSource));
            _relationshipDataSource = relationshipDataSource ?? throw new ArgumentNullException(nameof(relationshipDataSource));
            _statusDataSource = statusDataSource ?? throw new ArgumentNullException(nameof(statusDataSource));
            _dsHelper = dsHelper ?? throw new ArgumentNullException(nameof(dsHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessNotificationAsync(string diagnosticSettingsId, string monitorId, string tenantId, string operationType)
        {
            if (diagnosticSettingsId == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsId));
            }

            using var operation = _logger.StartTimedOperation(nameof(ProcessNotificationAsync));
            operation.SetContextProperty("WhaleRole", "NotificationManager");
            operation.SetContextProperty(nameof(diagnosticSettingsId), diagnosticSettingsId);
            operation.SetContextProperty(nameof(monitorId), monitorId);
            operation.SetContextProperty(nameof(tenantId), tenantId);

            _logger.Information("Started Processing Notification for Diagnostic Settings {diagnosticSettingsId}, pointing to monitor {monitorId}", diagnosticSettingsId, monitorId);

            var parsedMonitorId = new ResourceId(diagnosticSettingsId);
            var isUpdateOnV2Subscription = _subVersionSelector.IsV2Subscription(parsedMonitorId.SubscriptionId);

            if (!isUpdateOnV2Subscription)
            {
                _logger.Information("Unexpected case. Diagnostic Settings {diagnosticSettingsId} is not under a V2 subscription. No action to be done.", diagnosticSettingsId);
                return;
            }

            _logger.Information("Diagnostic Settings {diagnosticSettingsId} is under a V2 subscription.", diagnosticSettingsId);

            var getResult = await _diagnosticManager.GetResourceDiagnosticSettingsAsync(diagnosticSettingsId, tenantId);

            if (getResult.DiagnosticSettingV2Model == null || !getResult.SuccessfulOperation)
            {
                _logger.Information("Diagnostic Settings {diagnosticSettingsId} has been deleted.", diagnosticSettingsId);

                // TODO: Verify that operationType is compatible with if condition (i.e. when DiagnosticSettingV2Model == null then it's a Delete operationType)
                await ProcessDeleteNotificationAsync(diagnosticSettingsId, monitorId, parsedMonitorId.SubscriptionId, tenantId);
            }
            else
            {
                _logger.Information("Diagnostic Settings {diagnosticSettingsId} has been added/updated.", diagnosticSettingsId);
                await ProcessWriteNotificationAsync(diagnosticSettingsId, monitorId, parsedMonitorId.SubscriptionId, tenantId, getResult.DiagnosticSettingV2Model);
            }
        }

        protected abstract Task RestoreLiftrDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation);

        private async Task ProcessWriteNotificationAsync(string diagnosticSettingsId, string monitorId, string subscriptionId, string tenantId, DiagnosticSettingsModel dsV2Model)
        {
            using var operation = _logger.StartTimedOperation(nameof(ProcessWriteNotificationAsync));
            operation.SetContextProperty("NotificationType", "Write");
            _logger.Information("Started Processing Write Notification for Diagnostic Settings {diagnosticSettingsId}");

            var partnerResourceEntity = (await _partnerDataSource.ListAsync(monitorId)).FirstOrDefault();

            if (partnerResourceEntity == null)
            {
                // This case can be explained by either:
                // 1. delayed write notification processing. By the time of the processing, the monitor has been deleted.
                // 2. the DS points to a monitor in a different region.
                _logger.Information("Diagnostic Settings {diagnosticSettingsId} points to a monitor that has been deleted from Liftr's DB: {monitorId}.", diagnosticSettingsId, monitorId);
                return;
            }

            var partnerEntityId = partnerResourceEntity.EntityId;
            var monitoredResourceId = _dsHelper.ExtractMonitoredResourceId(diagnosticSettingsId).ToUpperInvariant();
            var diagnosticSettingsName = _dsHelper.ExtractDiagnosticSettingsName(diagnosticSettingsId);
            operation.SetContextProperty(nameof(monitoredResourceId), monitoredResourceId);
            operation.SetContextProperty(nameof(diagnosticSettingsName), diagnosticSettingsName);

            var statusEntity = await _statusDataSource.GetAsync(tenantId, partnerEntityId, monitoredResourceId);
            var isDSCreatedByLiftr = statusEntity != null && statusEntity.Reason != MonitoringStatusReason.CreatedByUser.GetReasonName();

            var relationshipModel = new MonitoringRelationshipModel(partnerEntityId, monitorId, monitoredResourceId, diagnosticSettingsId, diagnosticSettingsName, tenantId);
            if (isDSCreatedByLiftr)
            {
                var isAnyCategoryDisabled = dsV2Model.Properties.Logs.Any(logCategory => logCategory.Enabled == false);
                if (isAnyCategoryDisabled)
                {
                    _logger.Information("Liftr managed Diagnostic Settings {diagnosticSettingsId} has been altered (not all log categories selected). Reverting the change.", relationshipModel.DiagnosticSettingsId);
                    await RestoreLiftrDiagnosticSettingsAsync(relationshipModel, operation);
                }
                else
                {
                    _logger.Information("DB entities already exist for resource {monitoredResourceId}. No futher action needed.", relationshipModel.MonitoredResourceId);
                }
            }
            else
            {
                await SyncDBEntitiesWithUserDiagnosticSettingsAsync(relationshipModel, operation);
            }
        }

        private async Task ProcessDeleteNotificationAsync(string diagnosticSettingsId, string monitorId, string subscriptionId, string tenantId)
        {
            using var operation = _logger.StartTimedOperation(nameof(ProcessDeleteNotificationAsync));
            operation.SetContextProperty("NotificationType", "Delete");
            _logger.Information("Started Processing Delete Notification for Diagnostic Settings {diagnosticSettingsId}", diagnosticSettingsId);

            var partnerResourceEntity = (await _partnerDataSource.ListAsync(monitorId)).FirstOrDefault();

            if (partnerResourceEntity == null)
            {
                // This case will happen on Monitor deletion. DS are deleted before DB entties are deleted.
                // In this scenario, DB entities have been deleted before having received the DS delete notification.
                _logger.Information("Monitor DB entity has been already deleted. DB entities will be cleaned by Whale. No further action required.");
                return;
            }

            var partnerEntityId = partnerResourceEntity.EntityId;
            var monitoredResourceId = _dsHelper.ExtractMonitoredResourceId(diagnosticSettingsId).ToUpperInvariant();
            var diagnosticSettingsName = _dsHelper.ExtractDiagnosticSettingsName(diagnosticSettingsId);
            operation.SetContextProperty(nameof(monitoredResourceId), monitoredResourceId);
            operation.SetContextProperty(nameof(diagnosticSettingsName), diagnosticSettingsName);

            var statusEntity = await _statusDataSource.GetAsync(tenantId, partnerEntityId, monitoredResourceId);
            var isDSCreatedByLiftr = statusEntity != null && statusEntity.Reason != MonitoringStatusReason.CreatedByUser.GetReasonName();
            operation.SetContextProperty(nameof(isDSCreatedByLiftr), isDSCreatedByLiftr.ToString(CultureInfo.InvariantCulture));

            var relationshipModel = new MonitoringRelationshipModel(partnerEntityId, monitorId, monitoredResourceId, diagnosticSettingsId, diagnosticSettingsName, tenantId);

            if (isDSCreatedByLiftr)
            {
                await RestoreLiftrDiagnosticSettingsAsync(relationshipModel, operation);
            }
            else
            {
                await DeleteRelationshipEntityOfDeletedUserDiagnosticSettingsAsync(relationshipModel, operation);
            }
        }

        // This method will diverge between resource/subscription DS once we support log categories. It should then be moved to derived classes
        private async Task SyncDBEntitiesWithUserDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation)
        {
            _logger.Information("Diagnostic Settings {diagnosticSettingsId} is managed by the user.", relationshipModel.DiagnosticSettingsId);
            await RemoveDBEntitiesForRemovedDiagnosticSettingsAsync(relationshipModel, operation);
            await AddDBEntitiesIfNeededAsync(relationshipModel, operation);
        }

        // Check for DB entities of Diagnostic Settings that no longer exist. This should address 2 cases:
        // 1. The user updated a the monitor destination of DS. In that case the entities of the old monitor should be removed.
        // 2. Handles failures on proceessing delete notifications.
        private async Task RemoveDBEntitiesForRemovedDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation)
        {
            if (relationshipModel == null)
            {
                throw new ArgumentNullException(nameof(relationshipModel));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _logger.Information("Checking if DB relationship and status entities need to be removed.");

            var statusEntities = await _statusDataSource.ListByMonitoredResourceAsync(relationshipModel.TenantId, relationshipModel.MonitoredResourceId);
            var userStatusEntities = statusEntities.Where(entity => entity.Reason == MonitoringStatusReason.CreatedByUser.GetReasonName());

            if (!userStatusEntities.Any())
            {
                _logger.Information("No user DB entities exist. Nothing to remove.");
                return;
            }

            var getDSListResult = await _diagnosticManager.ListResourceDiagnosticSettingsAsync(relationshipModel.MonitoredResourceId, relationshipModel.TenantId);

            if (!getDSListResult.SuccessfulOperation)
            {
                _logger.Error("Failed at getting Diagnostic Settings list for resource {monitoredResourceId}.", relationshipModel.MonitoredResourceId);
                operation.FailOperation();
                throw new InvalidOperationException($"Failed at getting Diagnostic Settings list for resource {relationshipModel.MonitoredResourceId}.");
            }

            var diagnosticSettingsList = getDSListResult.DiagnosticSettingV2ModelList;

            foreach (var userEntity in userStatusEntities)
            {
                await RemoveDBEntitiesForRemovedDiagnosticSettingsAsync(relationshipModel, diagnosticSettingsList, userEntity);
            }
        }

        private async Task RemoveDBEntitiesForRemovedDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, List<DiagnosticSettingsModel> diagnosticSettingsList, MonitoringStatus userStatusEntity)
        {
            var partnerEntity = await _partnerDataSource.GetAsync(userStatusEntity.PartnerEntityId);
            var relEntity = await _relationshipDataSource.GetAsync(relationshipModel.TenantId, userStatusEntity.PartnerEntityId, relationshipModel.MonitoredResourceId);

            if (relEntity == null)
            {
                _logger.Warning("Unexpected case. No relationship entity associated with the status entity.");
                return;
            }

            var dsId = _dsHelper.BuildDiagnosticSettingsID(relationshipModel.MonitoredResourceId, relEntity.DiagnosticSettingsName);

            // Check if there's an actual DS corresponding to the DB entities.
            var entityHasDiagnosticSettings = diagnosticSettingsList != null && diagnosticSettingsList.Any(ds =>
            {
                return ds.Id.Equals(dsId, StringComparison.OrdinalIgnoreCase) && ds.Properties.MarketplacePartnerId.Equals(partnerEntity.EntityId, StringComparison.OrdinalIgnoreCase);
            });

            if (!entityHasDiagnosticSettings)
            {
                _logger.Information("User Diagnostic Settings {dsID} pointing to monitor {partnerEntity.EntityId} no longer exists. Removing status and relationship entities", dsId, partnerEntity.EntityId);
                await _relationshipDataSource.DeleteAsync(relationshipModel.TenantId, userStatusEntity.PartnerEntityId, userStatusEntity.MonitoredResourceId);
                await _statusDataSource.DeleteAsync(relationshipModel.TenantId, userStatusEntity.PartnerEntityId, userStatusEntity.MonitoredResourceId);
            }
        }

        private async Task AddDBEntitiesIfNeededAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation)
        {
            if (relationshipModel == null)
            {
                throw new ArgumentNullException(nameof(relationshipModel));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _logger.Information("Checking if DB relationship and status entities need to be created.");

            var monitoringRelationship = await _relationshipDataSource.GetAsync(relationshipModel.TenantId, relationshipModel.PartnerEntityId, relationshipModel.MonitoredResourceId);

            if (monitoringRelationship == null)
            {
                try
                {
                    _logger.Information("No existing DB entities for resource {monitoredResourceId}. Adding them.", relationshipModel.MonitoredResourceId);

                    var statusEntity = new Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus()
                    {
                        PartnerEntityId = relationshipModel.PartnerEntityId,
                        Reason = MonitoringStatusReason.CreatedByUser.GetReasonName(),
                        MonitoredResourceId = relationshipModel.MonitoredResourceId,
                        TenantId = relationshipModel.TenantId,
                        IsMonitored = true,
                    };

                    await _statusDataSource.AddOrUpdateAsync(statusEntity);
                    _logger.Information("Added status entity.");

                    var relationshipEntity = new MonitoringRelationship()
                    {
                        PartnerEntityId = relationshipModel.PartnerEntityId,
                        MonitoredResourceId = relationshipModel.MonitoredResourceId,
                        TenantId = relationshipModel.TenantId,
                        DiagnosticSettingsName = relationshipModel.DiagnosticSettingsName,
                    };

                    await _relationshipDataSource.AddAsync(relationshipEntity);
                    _logger.Information("Added relationship entity.");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.Error("Failed at adding DB entities for Diagnostic Settings {diagnosticSettingsId}.", relationshipModel.DiagnosticSettingsId, ex);
                    operation.FailOperation();
                }
            }
            else
            {
                _logger.Information("DB entities already exist for resource {monitoredResourceId}. No futher action needed.", relationshipModel.MonitoredResourceId);
            }
        }

        private async Task DeleteRelationshipEntityOfDeletedUserDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation)
        {
            if (relationshipModel == null)
            {
                throw new ArgumentNullException(nameof(relationshipModel));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _logger.Information("Resource Diagnostic Settings {diagnosticSettingsId} is managed by the user. Checking if DB relationship and status entities need to be deleted.", relationshipModel.DiagnosticSettingsId);

            var monitoringRelationship = await _relationshipDataSource.GetAsync(relationshipModel.TenantId, relationshipModel.PartnerEntityId, relationshipModel.MonitoredResourceId);
            if (monitoringRelationship == null)
            {
                _logger.Information("No existing DB entities for resource {monitoredResourceId}. No futher action needed.", relationshipModel.MonitoredResourceId);
                return;
            }
            else
            {
                _logger.Information("DB entities already exist for resource {monitoredResourceId}. Deleting them.", relationshipModel.MonitoredResourceId);
                try
                {
                    await _statusDataSource.DeleteAsync(relationshipModel.TenantId, relationshipModel.PartnerEntityId, relationshipModel.MonitoredResourceId);
                    _logger.Information("Deleted status entity.");

                    await _relationshipDataSource.DeleteAsync(relationshipModel.TenantId, relationshipModel.PartnerEntityId, relationshipModel.MonitoredResourceId);
                    _logger.Information("Deleted relationship entity.");
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed at deleting DB entities for Diagnostic Settings {diagnosticSettingsId}.", relationshipModel.DiagnosticSettingsId, ex);
                    operation.FailOperation();
                    throw;
                }
            }
        }
    }

    public class MonitoringRelationshipModel
    {
        public MonitoringRelationshipModel(string partnerEntityId, string monitorId, string monitoredResourceId, string diagnosticSettingsId, string diagnosticSettingsName, string tenantId)
        {
            PartnerEntityId = partnerEntityId;
            MonitorId = monitorId;
            MonitoredResourceId = monitoredResourceId;
            DiagnosticSettingsId = diagnosticSettingsId;
            DiagnosticSettingsName = diagnosticSettingsName;
            TenantId = tenantId;
        }

        public string PartnerEntityId { get; set; }

        public string MonitorId { get; set; }

        public string MonitoredResourceId { get; set; }

        public string DiagnosticSettingsId { get; set; }

        public string DiagnosticSettingsName { get; set; }

        public string TenantId { get; set; }
    }
}