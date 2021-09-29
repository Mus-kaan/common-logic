//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders
{
    public class DiagnosticSettingsSubscriptionModelBuilder : DiagnosticSettingsModelBuilderBase
    {
        public DiagnosticSettingsSubscriptionModelBuilder()
        {
        }

        protected override async Task<List<DiagnosticSettingsLogsOrMetricsModel>> BuildAllLogsDiagnosticSettingsPropertyAsync(IArmClient _armClient, string monitoredResourceId, string DiagnosticSettingsV2ApiVersion, string tenantId)
        {
            var res = Constants.SubscriptionLogCategories.Select(category =>
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