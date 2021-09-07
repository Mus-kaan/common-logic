//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.Common.Interfaces;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Monitoring.Whale.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Notifications
{
    public class SubscriptionNotificationManager : NotificationManagerBase
    {
        public SubscriptionNotificationManager(
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
        }

        protected override async Task RestoreLiftrDiagnosticSettingsAsync(MonitoringRelationshipModel relationshipModel, ITimedOperation operation)
        {
            if (relationshipModel == null)
            {
                throw new ArgumentNullException(nameof(relationshipModel));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _logger.Information("Restoring the Liftr managed Subscription Diagnostic Settings {diagnosticSettingsId}.");
            var dsAddResult = await _diagnosticManager.CreateOrUpdateResourceDiagnosticSettingAsync(relationshipModel.MonitoredResourceId, relationshipModel.DiagnosticSettingsName, relationshipModel.MonitorId, relationshipModel.TenantId);

            if (dsAddResult.SuccessfulOperation)
            {
                _logger.Information("Subscription Diagnostic Settings {diagnosticSettingsId} restored successfully.", relationshipModel.DiagnosticSettingsId);
            }
            else
            {
                _logger.Error("Failed at re-creating Subscription Diagnostic Settings {diagnosticSettingsId}.", relationshipModel.DiagnosticSettingsId);
                var statusEntity = new Microsoft.Liftr.DataSource.Mongo.MonitoringSvc.MonitoringStatus()
                {
                    PartnerEntityId = relationshipModel.PartnerEntityId,
                    MonitoredResourceId = relationshipModel.MonitoredResourceId.ToUpperInvariant(),
                    TenantId = relationshipModel.TenantId,
                    Reason = MonitoringStatusReason.Other.GetReasonName(),
                };

                await _statusDataSource.AddOrUpdateAsync(statusEntity);
                operation.FailOperation();
            }
        }
    }
}