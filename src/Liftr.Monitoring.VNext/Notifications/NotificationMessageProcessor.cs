//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using Microsoft.Liftr.Monitoring.Notifications.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.Notifications
{
    public class NotificationMessageProcessor : INotificationMessageProcessor
    {
        private readonly SubscriptionNotificationManager _subscriptionNotificationManager;
        private readonly ResourceNotificationManager _resourceNotificationManager;
        private readonly TenantNotificationManager _tenantNotificationManager;

        private readonly IMonitoringRelationshipDataSource<MonitoringRelationship> _relationshipDataSource;
        private readonly IPartnerResourceDataSource<PartnerResourceEntity> _partnerDataSource;
        private readonly DiagnosticSettingsHelper _dsHelper;
        private readonly IDiagnosticSettingsNameProvider _dsNameProvider;
        private readonly ILogger _logger;

        public NotificationMessageProcessor(
            SubscriptionNotificationManager subscriptionNotificationManager,
            ResourceNotificationManager resourceNotificationManager,
            TenantNotificationManager tenantNotificationManager,
            IMonitoringRelationshipDataSource<MonitoringRelationship> relationshipDataSource,
            IPartnerResourceDataSource<PartnerResourceEntity> partnerDataSource,
            DiagnosticSettingsHelper dsHelper,
            IDiagnosticSettingsNameProvider dsNameProvider,
            ILogger logger)
        {
            _subscriptionNotificationManager = subscriptionNotificationManager ?? throw new ArgumentNullException(nameof(subscriptionNotificationManager));
            _resourceNotificationManager = resourceNotificationManager ?? throw new ArgumentNullException(nameof(resourceNotificationManager));
            _tenantNotificationManager = tenantNotificationManager ?? throw new ArgumentNullException(nameof(tenantNotificationManager));
            _relationshipDataSource = relationshipDataSource ?? throw new ArgumentNullException(nameof(relationshipDataSource));
            _partnerDataSource = partnerDataSource ?? throw new ArgumentNullException(nameof(partnerDataSource));
            _dsHelper = dsHelper ?? throw new ArgumentNullException(nameof(dsHelper));
            _dsNameProvider = dsNameProvider ?? throw new ArgumentNullException(nameof(dsNameProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessNotificationQueueMessageAsync(DiagnosticSettingsNotification dsNotification)
        {
            if (dsNotification == null)
            {
                throw new ArgumentNullException(nameof(dsNotification));
            }

            if (dsNotification.DiagnosticSettingsId == null)
            {
                throw new ArgumentException("DiagnosticSettingsId cannot be null", nameof(dsNotification));
            }

            if (dsNotification.TenantId == null)
            {
                throw new ArgumentException("TenantId cannot be null", nameof(dsNotification));
            }

            if (dsNotification.EventType == null)
            {
                throw new ArgumentException("EventType", nameof(dsNotification));
            }

            var monitorId = await GetMonitorIdFromNotificationAsync(dsNotification);

            if (monitorId != null)
            {
                await ProcessNotificationMessageAsync(dsNotification);
            }
        }

        private async Task<string> GetMonitorIdFromNotificationAsync(DiagnosticSettingsNotification dsNotification)
        {
            using var op = _logger.StartTimedOperation(nameof(GetMonitorIdFromNotificationAsync), skipAppInsights: true);
            var monitorId = dsNotification.MonitorId;
            var diagnosticSettingsId = dsNotification.DiagnosticSettingsId;
            var eventType = dsNotification.EventType;
            var tenantId = dsNotification.TenantId;
            op.SetContextProperty(nameof(monitorId), monitorId);
            op.SetContextProperty(nameof(diagnosticSettingsId), diagnosticSettingsId);
            op.SetContextProperty(nameof(eventType), eventType);
            op.SetContextProperty(nameof(tenantId), tenantId);

            if (_dsHelper.DoesDiagnosticSettingsBelongToTenant(diagnosticSettingsId))
            {
                if(monitorId == null)
                {
                    var dsName = _dsHelper.ExtractDiagnosticSettingsNameForAAD(diagnosticSettingsId).ToLower(CultureInfo.InvariantCulture);
                    monitorId = await FindMonitorNameFromDiagnosticSettingsIdAsync(dsName, tenantId, tenantId);
                    dsNotification.MonitorId = monitorId;
                
                }
                return monitorId;
            }
            // TODO: remove the logic depending on eventType
            // Events on the same DS are not guaranteed to be ordered and therefore we shouldn't rely on eventType.
            // Instead, we should query the actual DiagnosticSetting and use its state.
            // The only reason we're making this distinction now is because the `delete` messages are missing the `monitorId` field.
            // We requested ARN team to add this field to `delete` notification which will be surfaced to this message. Once/If this is done, we can simplify this logic making and remove distinction between `write` and `delete`
            if (eventType.ToLower(CultureInfo.InvariantCulture).Equals("microsoft.insights/diagnosticSettings/write", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Information("Notification Event is of type `Write`");
                var prefixedProviderName = _dsNameProvider.GetPrefixedResourceProviderName();
                var prefixedProviderNameObservability = _dsNameProvider.GetPrefixedWithObservabilityResourceProviderName();
                return !string.IsNullOrEmpty(monitorId) &&
                    (monitorId.OrdinalContains(prefixedProviderName) || monitorId.OrdinalContains(prefixedProviderNameObservability)) ? monitorId : null;
            }

            if (eventType.ToLower(CultureInfo.InvariantCulture).Equals("microsoft.insights/diagnosticSettings/delete", StringComparison.OrdinalIgnoreCase))
            {
                var monitoredResourceId = _dsHelper.ExtractMonitoredResourceId(diagnosticSettingsId);
                var dsName = _dsHelper.ExtractDiagnosticSettingsName(diagnosticSettingsId).ToLower(CultureInfo.InvariantCulture);
                
                op.SetContextProperty("diagnosticSettingsName", dsName);
                var monitorResourceId = await FindMonitorNameFromDiagnosticSettingsIdAsync(dsName,tenantId,monitoredResourceId.ToUpperInvariant());
                dsNotification.MonitorId = monitorResourceId;
                return monitorResourceId;
            }
                

            throw new ArgumentException($"Unexpected Notification EventType value {eventType}");
        }

        private async Task<string> FindMonitorNameFromDiagnosticSettingsIdAsync(string dsName, string tenantId,string monitoredResourceId)
        {
            var monitoringRelationships = await _relationshipDataSource.ListByMonitoredResourceAsync(tenantId, monitoredResourceId);
            if (monitoringRelationships == null)
            {
                // Monitor is in a different region
                _logger.Information("Diagnostic Settings {diagnosticSettingsId} is not in the DB.");
                return null;
            }
            _logger.Information("Relationships in the database are {@Relationships} {NumberOfRelationships}.",monitoringRelationships,monitoringRelationships.Count());
            var rel = monitoringRelationships.FirstOrDefault(rel => rel.DiagnosticSettingsName.ToLower(CultureInfo.InvariantCulture).Equals(dsName));

            if (rel == null)
            {
                // Monitor is in a different region
                _logger.Information("Diagnostic Settings with dsName {dsName} is not  the DB.",dsName);
                return null;
            }

            var parnterResource = await _partnerDataSource.GetAsync(rel.PartnerEntityId);
            _logger.Information("Diagnostic Settings {dsName} has the monitor destination {monitorId}.", dsName, parnterResource.ResourceId);

            return parnterResource.ResourceId;
        }

        private async Task ProcessNotificationMessageAsync(DiagnosticSettingsNotification dsNotification)
        {
            try
            {
                _logger.Information("Started Processing notification Message.");

                var notificationManager = GetNotificationManager(dsNotification.DiagnosticSettingsId);
                await notificationManager.ProcessNotificationAsync(dsNotification.DiagnosticSettingsId, dsNotification.MonitorId, dsNotification.TenantId, dsNotification.EventType);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(ex, "Failed at Processing notification.");
            }
        }

        private INotificationManager GetNotificationManager(string diagnosticSettingsId)
        {
            if (_dsHelper.DoesDiagnosticSettingsBelongToTenant(diagnosticSettingsId))
            {
                _logger.Information("Diagnostic Settings {diagnosticSettingsId} belongs to tenant", diagnosticSettingsId);
                return _tenantNotificationManager;
            }

            if (_dsHelper.DoesDiagnosticSettingsBelongToSubscription(diagnosticSettingsId))
            {
                _logger.Information("Diagnostic Settings {diagnosticSettingsId} belongs to subscription", diagnosticSettingsId);
                return _subscriptionNotificationManager;
            }
            _logger.Information("Diagnostic Settings {diagnosticSettingsId} belongs to resource", diagnosticSettingsId);
            return _resourceNotificationManager;
        }
    }
}