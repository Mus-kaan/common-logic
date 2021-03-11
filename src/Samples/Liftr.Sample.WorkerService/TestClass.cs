//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Metrics;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Liftr.Sample.WorkerService
{
    public class TestClass : ITestClass
    {
        private readonly ILogger _logger;

        public TestClass(ILogger logger)
        {
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [LiftrMetrics("Test_Metric")]
        public DateTime TestMethod()
        {
            var dateTime = DateTime.Now;

            _logger.Information("The Real Method is Executed Here!");

            return dateTime;
        }

        [LiftrMetrics("Test_Metric_Async")]
        public Task<DateTime> TestMethodAsync()
        {
            var dateTime = DateTime.Now;

            _logger.Information("The Real Async Method is Executed Here!");

            return Task.FromResult(dateTime);
        }

        [LiftrMetrics("Test_Metric_Async_Exception")]
        public Task<DateTime> TestMethodWithExceptionAsync()
        {
            var dateTime = DateTime.Now;
            Task.Delay(10);
#pragma warning disable CA2201 // Do not raise reserved exception types
            throw new Exception("Test Exception");
#pragma warning restore CA2201 // Do not raise reserved exception types
        }
    }
}
