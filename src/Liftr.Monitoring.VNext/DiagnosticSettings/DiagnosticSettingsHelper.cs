//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces;
using Microsoft.Liftr.Contracts;
using Serilog;
using System;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings
{
    public class DiagnosticSettingsHelper
    {
        private const string DiagnosticSettingsProvider = "/providers/microsoft.insights/diagnosticSettings";
        private readonly IDiagnosticSettingsNameProvider _dsNameProvider;
        private readonly ILogger _logger;

        public DiagnosticSettingsHelper(IDiagnosticSettingsNameProvider dsNameProvider, ILogger logger)
        {
            _dsNameProvider = dsNameProvider ?? throw new ArgumentNullException(nameof(dsNameProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ExtractMonitoredResourceId(string diagnosticSettingsId)
        {
            if (string.IsNullOrWhiteSpace(diagnosticSettingsId))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsId));
            }

            _logger.Information("Extracting MonitoredResourceId for Diagnostic Settings {diagnosticSettingsId}.", diagnosticSettingsId);
            var ind = diagnosticSettingsId.IndexOf(DiagnosticSettingsProvider, StringComparison.OrdinalIgnoreCase);

            if (ind == -1)
            {
                throw new FormatException("Invalid Diagnostic Settings ID format");
            }

            var monitoredResourceId = diagnosticSettingsId.Substring(0, ind);

            _logger.Information("Finished extracting MonitoredResourceId for Diagnostic Settings {diagnosticSettingsId}. Value is {MonitoredResourceId}", diagnosticSettingsId, monitoredResourceId);
            return monitoredResourceId;
        }

        public string ExtractDiagnosticSettingsName(string diagnosticSettingsId)
        {
            if (string.IsNullOrWhiteSpace(diagnosticSettingsId))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsId));
            }

            _logger.Information("Extracting DiagnosticSettingsName for Diagnostic Settings {diagnosticSettingsId}.", diagnosticSettingsId);
            var ind = diagnosticSettingsId.IndexOf(DiagnosticSettingsProvider, StringComparison.OrdinalIgnoreCase);

            if (ind == -1)
            {
                throw new FormatException("Invalid Diagnostic Settings ID format");
            }

            var DiagnosticSettingsName = diagnosticSettingsId.Substring(ind + DiagnosticSettingsProvider.Length + 1);

            _logger.Information("Finished extracting DiagnosticSettingsName for Diagnostic Settings {diagnosticSettingsId}. Value is {DiagnosticSettingsName}", diagnosticSettingsId, DiagnosticSettingsName);
            return DiagnosticSettingsName;
        }

        public bool DoesDiagnosticSettingsBelongToSubscription(string diagnosticSettingsId)
        {
            if (string.IsNullOrWhiteSpace(diagnosticSettingsId))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsId));
            }

            string monitoredResourceId = ExtractMonitoredResourceId(diagnosticSettingsId);
            var parsedResourceId = new ResourceId(monitoredResourceId);
            return monitoredResourceId.EndsWith(parsedResourceId.SubscriptionId, StringComparison.OrdinalIgnoreCase);
        }

        public string ExtractFullyQualifiedResourceProviderType(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            var parsedResourceId = new ResourceId(resourceId);
            return $"{parsedResourceId.Provider}/{parsedResourceId.ResourceType}";
        }

        public string BuildDiagnosticSettingsID(string monitoredResourceId, string diagnosticSettingsName)
        {
            if (string.IsNullOrWhiteSpace(monitoredResourceId))
            {
                throw new ArgumentNullException(nameof(monitoredResourceId));
            }

            if (string.IsNullOrWhiteSpace(diagnosticSettingsName))
            {
                throw new ArgumentNullException(nameof(diagnosticSettingsName));
            }

            return $"{monitoredResourceId.ToLowerInvariant()}/{_dsNameProvider.GetPrefixedResourceProviderName()}/{diagnosticSettingsName.ToLowerInvariant()}";
        }
    }
}