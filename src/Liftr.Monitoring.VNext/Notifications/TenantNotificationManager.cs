//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.Common.Interfaces;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Notifications
{
    public class TenantNotificationManager : NotificationManagerBase
    {
        private readonly DiagnosticSettingsHelper _dsHelper;

        public TenantNotificationManager(
            IWhaleFilterClient whaleFilterClient,
            ISubscriptionVersionSelector subVersionSelector,
            IDiagnosticSettingsManager diagnosticManager,
            IPartnerResourceDataSource<PartnerResourceEntity> partnerDataSource,
            IMonitoringRelationshipDataSource<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringRelationship> relationshipDataSource,
            IMonitoringStatusDataSource<Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus> statusDataSource,
            DiagnosticSettingsHelper dsHelper,
            ILogger logger)
            : base(
                whaleFilterClient,
                subVersionSelector,
                diagnosticManager,
                partnerDataSource,
                relationshipDataSource,
                statusDataSource,
                dsHelper,
                logger)
        {
            _dsHelper = dsHelper;
        }

        public override async Task ProcessNotificationAsync(string diagnosticSettingsId, string monitorId, string tenantId, string operationType)
        {
            if (diagnosticSettingsId == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsId));
            }
            if (monitorId == null)
            {
                throw new ArgumentNullException(nameof(monitorId));
            }
            if (tenantId == null)
            {
                throw new ArgumentNullException(nameof(tenantId));
            }
            if (operationType == null)
            {
                throw new ArgumentNullException(nameof(operationType));
            }
            using var operation = _logger.StartTimedOperation(nameof(ProcessNotificationAsync));
            operation.SetContextProperty("WhaleRole", "NotificationManager");
            operation.SetContextProperty(nameof(diagnosticSettingsId), diagnosticSettingsId);
            operation.SetContextProperty(nameof(monitorId), monitorId);
            operation.SetContextProperty(nameof(tenantId), tenantId);

            _logger.Information("Started Processing tenant Notification for Diagnostic Settings {diagnosticSettingsId}, pointing to monitor {monitorId} operationType {operationType}", diagnosticSettingsId, monitorId,operationType);
           
            if (operationType.Contains("write", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessWriteNotificationForAAdAsync(diagnosticSettingsId, monitorId, tenantId);
            }
            else if (operationType.Contains("delete", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessDeleteNotificationForAAdAsync(diagnosticSettingsId, monitorId, tenantId);
            }
            else
            {
                _logger.Warning("UnExpected Operation name for diagnostic settings {diagnosticSettingsId} operationType {operationType}",diagnosticSettingsId,operationType);
            }

        }

        private async Task ProcessWriteNotificationForAAdAsync(string diagnosticSettingsId, string monitorId, string tenantId)
        {
            using var operation = _logger.StartTimedOperation(nameof(ProcessWriteNotificationForAAdAsync));
            operation.SetContextProperty("NotificationType", "Write");
            _logger.Information("Started Processing AAD Write Notification for Diagnostic Settings {diagnosticSettingsId}");

            var partnerResourceEntity = (await _partnerDataSource.ListAsync(monitorId)).FirstOrDefault();

            if (partnerResourceEntity == null)
            {
                // This case can be explained by either:
                // 1. delayed write notification processing. By the time of the processing, the monitor has been deleted.
                // 2. the DS points to a monitor in a different region.
                _logger.Information("Tenant Diagnostic Settings {diagnosticSettingsId} points to a monitor that has been deleted from Liftr's DB: {monitorId}.", diagnosticSettingsId, monitorId);
                return;
            }

            var partnerEntityId = partnerResourceEntity.EntityId;
            var monitoredResourceId = tenantId;
            var diagnosticSettingsName = _dsHelper.ExtractDiagnosticSettingsNameForAAD(diagnosticSettingsId);
            operation.SetContextProperty(nameof(monitoredResourceId), monitoredResourceId);
            operation.SetContextProperty(nameof(diagnosticSettingsName), diagnosticSettingsName);
            var relationshipModel = new MonitoringRelationshipModel(partnerEntityId, monitorId, monitoredResourceId, diagnosticSettingsId, diagnosticSettingsName, tenantId);
            await AddDBEntitiesIfNeededAsync(relationshipModel, operation);
            _logger.Information("successfully Processed AAD Write Notification for Diagnostic Settings {diagnosticSettingsId}");
        }

        private async Task ProcessDeleteNotificationForAAdAsync(string diagnosticSettingsId, string monitorId, string tenantId)
        {
            using var operation = _logger.StartTimedOperation(nameof(ProcessDeleteNotificationForAAdAsync));
            operation.SetContextProperty("NotificationType", "Delete");
            _logger.Information("Started Processing AAD Delete Notification for Diagnostic Settings {diagnosticSettingsId}");

            var partnerResourceEntity = (await _partnerDataSource.ListAsync(monitorId)).FirstOrDefault();

            if (partnerResourceEntity == null)
            {
                // This case can be explained by either:
                // 1. delayed write notification processing. By the time of the processing, the monitor has been deleted.
                // 2. the DS points to a monitor in a different region.
                _logger.Information("Tenant Diagnostic Settings {diagnosticSettingsId} points to a monitor that has been deleted from Liftr's DB: {monitorId}.", diagnosticSettingsId, monitorId);
                return;
            }

            var partnerEntityId = partnerResourceEntity.EntityId;
            var monitoredResourceId = tenantId;
            var diagnosticSettingsName = _dsHelper.ExtractDiagnosticSettingsNameForAAD(diagnosticSettingsId);
            operation.SetContextProperty(nameof(monitoredResourceId), monitoredResourceId);
            operation.SetContextProperty(nameof(diagnosticSettingsName), diagnosticSettingsName);
            var relationshipModel = new MonitoringRelationshipModel(partnerEntityId, monitorId, monitoredResourceId, diagnosticSettingsId, diagnosticSettingsName, tenantId);
            await DeleteRelationshipEntityOfDeletedUserDiagnosticSettingsAsync(relationshipModel, operation);
            _logger.Information("successfully Processed AAD Delete Notification for Diagnostic Settings {diagnosticSettingsId}");
        }

        protected override Task RestoreLiftrDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation)
        {
            throw new NotImplementedException();
        }
    }
}