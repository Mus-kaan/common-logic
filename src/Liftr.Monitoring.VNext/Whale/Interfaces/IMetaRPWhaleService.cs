//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.VNext.Whale.Models;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Whale.Interfaces
{
    /// <summary>
    /// Service Interface that each Liftr Observability Partner implement to fetch Monitor and TagRules details from MetaRP
    /// </summary>
    public interface IMetaRPWhaleService
    {
        /// <summary>
        /// Fetch TagRules from MetaRP
        /// <param name="monitorId">Parent ARM resource id of reource type monitor for tagrules</param>
        /// <param name="tenantId">The tenant Id where tagRules resource belong to</param>
        /// </summary>
        public Task<MonitoringTagRules> GetMonitoringTagRulesAsync(string monitorId, string tenantId);

        /// <summary>
        /// Fetch Monitor Resource details from MetaRP
        /// <param name="monitorId">ARM resource id of reource type monitor</param>
        /// <param name="tenantId">The tenant Id where tagRules resource belong to</param>
        /// </summary>
        public Task<MonitorResourceDetails> GetMonitorResourceDetailsAsync(string monitorId, string tenantId);
    }
}
