//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders
{
    public class DiagnosticSettingsSubscriptionModelBuilder : DiagnosticSettingsModelBuilderBase
    {
        private static readonly List<string> s_subscriptionLogCategories = new List<string> { "Administrative", "Security", "ServiceHealth", "Alert", "Recommendation", "Policy", "Autoscale", "ResourceHealth" };

        public DiagnosticSettingsSubscriptionModelBuilder()
        {
        }

        protected override async Task<List<DiagnosticSettingsLogsOrMetricsModel>> BuildAllLogsDiagnosticSettingsPropertyAsync(IAzure fluentClient, string monitoredResourceId)
        {
            var res = s_subscriptionLogCategories.Select(category =>
            {
                var logCategory = new DiagnosticSettingsLogsOrMetricsModel();
                logCategory.Category = category;
                logCategory.Enabled = true;
                return logCategory;
            }).ToList();

            return await Task.FromResult<List<DiagnosticSettingsLogsOrMetricsModel>>(res);
        }
    }
}