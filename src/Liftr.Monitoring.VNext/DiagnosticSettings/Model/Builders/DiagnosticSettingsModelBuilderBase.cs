//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1822:Member does not access instance data and can be marked as static", Justification = "<Pending>")]
    public abstract class DiagnosticSettingsModelBuilderBase
    {
        public async Task<string> BuildAllLogsAndNoMetricsDiagnosticSettingsBodyAsync(IAzure fluentClient, string monitoredResourceId, string diagnosticSettingsName, string monitorId)
        {
            var diagnosticSettingsId = DiagnosticSettingsManager.GetDiagnosticSettingsId(monitoredResourceId, diagnosticSettingsName);
            var model = new DiagnosticSettingsModel();
            model.Id = diagnosticSettingsId;
            model.Name = diagnosticSettingsName;
            model.Type = "Microsoft.Insights/diagnosticSettings";
            model.Properties = new DiagnosticSettingsPropertiesModel();
            model.Properties.MarketplacePartnerId = monitorId;
            model.Properties.Metrics = BuildNoMetricsDiagnosticSettingsProperty();
            model.Properties.Logs = await BuildAllLogsDiagnosticSettingsPropertyAsync(fluentClient, monitoredResourceId);

            return model.ToJsonString();
        }

        protected abstract Task<List<DiagnosticSettingsLogsOrMetricsModel>> BuildAllLogsDiagnosticSettingsPropertyAsync(IAzure fluentClient, string monitoredResourceId);

        private static List<DiagnosticSettingsLogsOrMetricsModel> BuildNoMetricsDiagnosticSettingsProperty()
        {
            return new List<DiagnosticSettingsLogsOrMetricsModel>();
        }
    }
}