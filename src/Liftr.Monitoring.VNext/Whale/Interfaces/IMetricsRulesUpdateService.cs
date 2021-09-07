//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Interfaces
{
    /// <summary>
    /// Service Interface that each Liftr Observability Partner implement to update metrics filter rule.
    /// </summary>
    public interface IMetricsRulesUpdateService
    {
        /// <summary>
        /// Update metric filter rules on receiving update tag rules message in whale worker. 
        /// <param name="monitorId">ARM resource id of reource type monitor</param>
        /// <param name="tenantId">The tenant Id where tagRules resource belong to</param>
        /// </summary>
        public Task UpdateMetricRulesAsync(string monitorId, string tenantId);
    }
}
