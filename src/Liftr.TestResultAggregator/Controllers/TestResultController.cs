//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Utilities;
using Prometheus;
using System;
using System.Collections.Concurrent;

namespace Microsoft.Liftr.TestResultAggregator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestResultController : ControllerBase
    {
        private static readonly SummaryConfiguration s_summaryConfig = GetSummaryConfig();
        private static readonly ConcurrentDictionary<string, Summary> s_summaries = new ConcurrentDictionary<string, Summary>();
        private static readonly Summary s_totalSummary = Prometheus.Metrics.CreateSummary("test_overview", "Overview for all the tests", s_summaryConfig);
        private Serilog.ILogger _logger;

        public TestResultController(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] TestResult testResult)
        {
            if (testResult == null)
            {
                throw new ArgumentNullException(nameof(testResult));
            }

            var summary = s_summaries.GetOrAdd(testResult.OperationName, (_) =>
            {
                var name = testResult.OperationName;
                var summaryName = PrometheusHelper.ConvertToPrometheusMetricsName("test_" + name + "DurationMilliseconds");
                var newSummary = Prometheus.Metrics
                    .CreateSummary(summaryName, testResult.HelpText, s_summaryConfig);
                return newSummary;
            });

            try
            {
                var result = testResult.IsFailure ? "failure" : "success";
                summary
                    .WithLabels(testResult.Component, result, testResult.TestClass, testResult.TestMethod, testResult.TestCloudType, testResult.TestAzureRegion, testResult.TestRegionCategory)
                    .Observe(testResult.DurationMilliseconds);

                s_totalSummary
                    .WithLabels(testResult.Component, result, testResult.TestClass, testResult.TestMethod, testResult.TestCloudType, testResult.TestAzureRegion, testResult.TestRegionCategory)
                    .Observe(testResult.DurationMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed at generating metric. testResult: {@testResult}", testResult);
            }
        }

        private static SummaryConfiguration GetSummaryConfig()
        {
            var summaryConfig = new SummaryConfiguration
            {
                Objectives = new[]
                    {
                        new QuantileEpsilonPair(0.5, 0.05),
                        new QuantileEpsilonPair(0.75, 0.05),
                        new QuantileEpsilonPair(0.99, 0.005),
                    },
                LabelNames = new[] { "component", "result", "test_class", "test_method", "cloud", "region", "region_category" },
            };

            return summaryConfig;
        }
    }
}
