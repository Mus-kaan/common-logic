//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1822:Member does not access instance data and can be marked as static", Justification = "<Pending>")]
    public abstract class DiagnosticSettingsModelBuilderBase
    {
        public async Task<string> BuildAllLogsAndNoMetricsDiagnosticSettingsBodyAsync(IArmClient _armClient, string monitoredResourceId, string diagnosticSettingsName, string monitorId, string DiagnosticSettingsV2ApiVersion, string tenantId)
        {
            var diagnosticSettingsId = DiagnosticSettingsManager.GetDiagnosticSettingsId(monitoredResourceId, diagnosticSettingsName);
            var model = new DiagnosticSettingsModel
            {
                Id = diagnosticSettingsId,
                Name = diagnosticSettingsName,
                Type = Constants.AzureResourceType,
                Properties = new DiagnosticSettingsPropertiesModel()
            };
            model.Properties.MarketplacePartnerId = monitorId;
            model.Properties.Metrics = BuildNoMetricsDiagnosticSettingsProperty();
            model.Properties.Logs = await BuildAllLogsDiagnosticSettingsPropertyAsync(_armClient, monitoredResourceId, DiagnosticSettingsV2ApiVersion, tenantId);

            return model.ToJsonString();
        }

        protected abstract Task<List<DiagnosticSettingsLogsOrMetricsModel>> BuildAllLogsDiagnosticSettingsPropertyAsync(IArmClient _armClient, string monitoredResourceId, string DiagnosticSettingsV2ApiVersion, string tenantId);

        private static List<DiagnosticSettingsLogsOrMetricsModel> BuildNoMetricsDiagnosticSettingsProperty()
        {
            return new List<DiagnosticSettingsLogsOrMetricsModel>();
        }
    }
}