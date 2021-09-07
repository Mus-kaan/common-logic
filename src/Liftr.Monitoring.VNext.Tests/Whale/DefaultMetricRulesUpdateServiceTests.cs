//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.Whale;
using Moq;
using Serilog;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Monitoring.VNext.Tests.Whale
{
    public class DefaultMetricRulesUpdateServiceTests
    {
        private readonly ILogger _logger;

        public DefaultMetricRulesUpdateServiceTests()
        {
            var loggerMock = new Mock<ILogger>();
            _logger = loggerMock.Object;
        }

        [Fact]
        public void DefaultMetricRulesUpdateService_InvalidParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultMetricRulesUpdateService(null));
        }

        [Fact]
        public async Task UpdateMetricRulesAsync_ExpectedBehaviorAsync()
        {
            var defaultMetricsUpdateService = new DefaultMetricRulesUpdateService(_logger);

            await defaultMetricsUpdateService.UpdateMetricRulesAsync("monitordId", "tenantId");
        }
    }
}
