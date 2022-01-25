//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model;
using Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders;
using Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings
{
    /// <summary>
    /// Manage Azure Monitor's Diagnostic Settings using ARM rest calls
    /// </summary>
    /// <remarks>
    /// https://docs.microsoft.com/en-us/rest/api/monitor/diagnosticsettings
    /// </remarks>
    public class DiagnosticSettingsManager : IDiagnosticSettingsManager
    {
        private readonly IArmClient _armClient;
        private readonly DiagnosticSettingsResourceModelBuilder _dsV2ResourceModelBuilder;
        private readonly DiagnosticSettingsSubscriptionModelBuilder _dsV2SubscriptionModelBuilder;
        private readonly IDiagnosticSettingsNameProvider _dsNameProvider;
        private readonly ILogger _logger;

        public DiagnosticSettingsManager(
            DiagnosticSettingsResourceModelBuilder dsV2ResourceModelBuilder,
            DiagnosticSettingsSubscriptionModelBuilder dsV2SubscriptionModelBuilder,
            IDiagnosticSettingsNameProvider dsNameProvider,
            IArmClient armClient,
            ILogger logger)
        {
            _dsV2ResourceModelBuilder = dsV2ResourceModelBuilder ?? throw new ArgumentNullException(nameof(dsV2ResourceModelBuilder));
            _dsV2SubscriptionModelBuilder = dsV2SubscriptionModelBuilder ?? throw new ArgumentNullException(nameof(dsV2SubscriptionModelBuilder));
            _dsNameProvider = dsNameProvider ?? throw new ArgumentNullException(nameof(dsNameProvider));
            _armClient = armClient ?? throw new ArgumentNullException(nameof(armClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static string GetDiagnosticSettingsId(string monitoredResourceId, string diagnosticSettingsName)
        {
            return $"{monitoredResourceId}/providers/microsoft.insights/diagnosticSettings/{diagnosticSettingsName}";
        }

        public static string GetDiagnosticSettingsListId(string monitoredResourceId)
        {
            return $"{monitoredResourceId}/providers/microsoft.insights/diagnosticSettings";
        }

        public async Task<IDiagnosticSettingsManagerResult> GetResourceDiagnosticSettingsAsync(string diagnosticSettingsId, string tenantId)
        {
            var parsedMonitorId = new ResourceId(diagnosticSettingsId);

            try
            {
                var responseBody = await _armClient.GetResourceAsync(diagnosticSettingsId, Constants.DiagnosticSettingsV2ApiVersion, tenantId);
                var result = DiagnosticSettingsManagerResult.SuccessfulResult();
                var diagnosticSettingsV2Model = responseBody.FromJson<DiagnosticSettingsModel>();
                result.DiagnosticSettingsName = diagnosticSettingsV2Model.Name;
                result.DiagnosticSettingV2Model = diagnosticSettingsV2Model;
                return result;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(
                    ex,
                    "Failed to get diagnostic settings {diagnositcSettingsId}. Exception {ex}",
                    diagnosticSettingsId);

                return DiagnosticSettingsManagerResult.FailedResult();
            }
        }

        public async Task<IDiagnosticSettingsManagerResult> ListResourceDiagnosticSettingsAsync(string monitoredResourceId, string tenantId)
        {
            var parsedMonitorId = new ResourceId(monitoredResourceId);

            try
            {
                var diagnosticSettingsList = GetDiagnosticSettingsListId(monitoredResourceId);
                var responseBody = await _armClient.GetResourceAsync(diagnosticSettingsList, Constants.DiagnosticSettingsV2ApiVersion, tenantId);
                var result = DiagnosticSettingsManagerResult.SuccessfulResult();
                var diagnosticSettingsV2ListModel = responseBody.FromJson<DiagnosticSettingsModelList>();
                result.DiagnosticSettingV2ModelList = diagnosticSettingsV2ListModel.Value;
                return result;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(
                    ex,
                    "Failed to get diagnostic settings list for resource {monitoredResourceId}. Exception {ex}",
                    monitoredResourceId);

                return DiagnosticSettingsManagerResult.FailedResult();
            }
        }

        public async Task<IDiagnosticSettingsManagerResult> CreateOrUpdateResourceDiagnosticSettingAsync(string monitoredResourceId, string monitorId, string tenantId)
        {
            if (string.IsNullOrEmpty(monitoredResourceId))
            {
                throw new ArgumentNullException(nameof(monitoredResourceId));
            }

            var diagnosticSettingsName = _dsNameProvider.GetDiagnosticSettingNameForResourceV2();
            return await PutDiagnosticSettingsAsync(monitoredResourceId, diagnosticSettingsName, monitorId, tenantId, _dsV2ResourceModelBuilder);
        }

        public async Task<IDiagnosticSettingsManagerResult> CreateOrUpdateResourceDiagnosticSettingAsync(string monitoredResourceId, string diagnosticSettingsName, string monitorId, string tenantId)
        {
            return await PutDiagnosticSettingsAsync(monitoredResourceId, diagnosticSettingsName, monitorId, tenantId, _dsV2ResourceModelBuilder);
        }

        public async Task<IDiagnosticSettingsManagerResult> RemoveResourceDiagnosticSettingAsync(string monitoredResourceId, string diagnosticSettingName, string tenantId)
        {
            if (string.IsNullOrEmpty(monitoredResourceId))
            {
                throw new ArgumentNullException(nameof(monitoredResourceId));
            }

            if (string.IsNullOrEmpty(diagnosticSettingName))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingName));
            }

            var parsedMonitoredResourceId = new ResourceId(monitoredResourceId);
            var diagnosticSettingsId = GetDiagnosticSettingsId(monitoredResourceId, diagnosticSettingName);

            try
            {
                await _armClient.DeleteResourceAsync(diagnosticSettingsId, Constants.DiagnosticSettingsV2ApiVersion, tenantId);
                return DiagnosticSettingsManagerResult.SuccessfulResult();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(
                    ex,
                    "Failed to delete diagnostic setting {diagnosticSettingName} to resource {monitoredResourceId}.",
                    diagnosticSettingName,
                    monitoredResourceId);

                return DiagnosticSettingsManagerResult.FailedResult();
            }
        }

        public async Task<IDiagnosticSettingsManagerResult> CreateOrUpdateSubscriptionDiagnosticSettingAsync(string monioredSubscriptionId, string monitorId, string tenantId)
        {
            if (string.IsNullOrEmpty(monioredSubscriptionId))
            {
                throw new ArgumentNullException(nameof(monioredSubscriptionId));
            }

            var diagnosticSettingsName = _dsNameProvider.GetDiagnosticSettingNameForResourceV2();
            return await PutDiagnosticSettingsAsync(monioredSubscriptionId, diagnosticSettingsName, monitorId, tenantId, _dsV2SubscriptionModelBuilder);
        }

        public async Task<IDiagnosticSettingsManagerResult> CreateOrUpdateSubscriptionDiagnosticSettingAsync(string monioredSubscriptionId, string diagnosticSettingsName, string monitorId, string tenantId)
        {
            return await PutDiagnosticSettingsAsync(monioredSubscriptionId, diagnosticSettingsName, monitorId, tenantId, _dsV2SubscriptionModelBuilder);
        }

        public async Task<IDiagnosticSettingsManagerResult> RemoveSubscriptionDiagnosticSettingAsync(string monioredSubscriptionId, string diagnosticSettingName, string monitorId, string tenantId)
        {
            if (string.IsNullOrEmpty(monioredSubscriptionId))
            {
                throw new ArgumentNullException(nameof(monioredSubscriptionId));
            }

            if (string.IsNullOrEmpty(diagnosticSettingName))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingName));
            }

            var diagnosticSettingsId = GetDiagnosticSettingsId(monioredSubscriptionId, diagnosticSettingName);

            try
            {
                await _armClient.DeleteResourceAsync(diagnosticSettingsId, Constants.DiagnosticSettingsV2ApiVersion, tenantId);
                return DiagnosticSettingsManagerResult.SuccessfulResult();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(
                    ex,
                    "Failed to delete diagnostic setting {diagnosticSettingName} on subscription {monioredSubscriptionId}.",
                    diagnosticSettingName,
                    monioredSubscriptionId);

                return DiagnosticSettingsManagerResult.FailedResult();
            }
        }

        private async Task<IDiagnosticSettingsManagerResult> PutDiagnosticSettingsAsync(string monitoredResourceId, string diagnosticSettingsName, string monitorId, string tenantId, DiagnosticSettingsModelBuilderBase dsModelBuilder)
        {
            if (string.IsNullOrEmpty(monitoredResourceId))
            {
                throw new ArgumentNullException(nameof(monitoredResourceId));
            }

            if (string.IsNullOrEmpty(diagnosticSettingsName))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsName));
            }

            var diagnosticSettingsId = GetDiagnosticSettingsId(monitoredResourceId, diagnosticSettingsName);

            string diagnosticSettingsBody;
            try
            {
                diagnosticSettingsBody = await dsModelBuilder.BuildAllLogsAndNoMetricsDiagnosticSettingsBodyAsync(_armClient, monitoredResourceId, diagnosticSettingsName, monitorId, Constants.DiagnosticSettingsV2ApiVersion, tenantId);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(
                    ex,
                    "Failed to build diagnostic setting model on resource {resourceId} with monitor {monitorId}.",
                    monitoredResourceId,
                    monitorId);

                return DiagnosticSettingsManagerResult.FailedResult();
            }

            try
            {
                // TODO: Refactor PutResourceAsync in Common to throw an exception containing StatusCode (similar to ILiftrAzureExtention)
                await _armClient.PutResourceAsync(diagnosticSettingsId, Constants.DiagnosticSettingsV2ApiVersion, diagnosticSettingsBody, tenantId);
                var result = DiagnosticSettingsManagerResult.SuccessfulResult();
                result.DiagnosticSettingsName = diagnosticSettingsName;
                return result;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(
                    ex,
                    "Failed to add diagnostic setting to resource {resourceId} with monitor {monitorId}",
                    monitoredResourceId,
                    monitorId);

                return DiagnosticSettingsManagerResult.FailedResult(ex.Message);
            }
        }
    }
}