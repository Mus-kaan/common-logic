//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.Whale.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Whale
{
    /// <summary>
    /// Default blank implementation for metrics filter rule update used by whale message processor.
    /// </summary>
    public class DefaultMetricRulesUpdateService : IMetricsRulesUpdateService
    {
        private readonly ILogger _logger;

        public DefaultMetricRulesUpdateService(
            ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task UpdateMetricRulesAsync(string monitorId, string tenantId)
        {
            _logger.Information($"Inside {nameof(UpdateMetricRulesAsync)} of default implementation. Nothing to do.");
            return Task.FromResult(true);
        }
    }
}
