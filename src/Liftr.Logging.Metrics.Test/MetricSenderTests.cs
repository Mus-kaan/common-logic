//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog.Core;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Liftr.Logging.Metrics.Test
{
    public class MetricSenderTests
    {
        [Fact]
        public void BucketWithOutDims()
        {
            string ns = "testNS";
            string metric = "testMetric";
            Bucket bucket = new Bucket(ns, metric);
            var res = bucket.ToJson();
            Assert.Equal("{\"Namespace\":\"testNS\",\"Metric\":\"testMetric\",\"Dims\":null}", res);
        }

        [Fact]
        public void BucketWithDims()
        {
            string ns = "testNS";
            string metric = "testMetric";
            Dictionary<string, string> dimension = new Dictionary<string, string>
            {
                ["testDimName1"] = "testDimVal1",
                ["testDimName2"] = "testDimVal2",
            };
            Bucket bucket = new Bucket(ns, metric, dimension);
            var res = bucket.ToJson();
            Assert.Equal("{\"Namespace\":\"testNS\",\"Metric\":\"testMetric\",\"Dims\":{\"testDimName1\":\"testDimVal1\",\"testDimName2\":\"testDimVal2\"}}", res);
        }

        [Fact]
        public void MetricSenderWithMetricsSendMetrics()
        {
            var dims = new Dictionary<string, string>
            {
                ["Environment"] = "Test",
                ["Location"] = "Test-EastUs",
                ["HostName"] = "TestMachine1",
            };

            MetricSender metricSender = new MetricSender("geneva-service", "MdmNamespace", Logger.None, dims);

            // Send without specific metrics
            metricSender.Gauge("testmetric", 1);

            // Send with specific metrics
            var specificDimension = new Dictionary<string, string>
            {
                ["testDimName1"] = "testDimVal1",
                ["testDimName2"] = "testDimVal2",
            };

            int value = 100;
            metricSender.Gauge("testmetric", value, specificDimension);
        }

        [Fact]
        public void MetricSenderWithoutMetricsSendMetrics()
        {
            MetricSender metricSender = new MetricSender("geneva-service", "MdmNamespace", Logger.None, null);

            // Send without specific metrics
            metricSender.Gauge("testmetric", 1);

            // Send with specific metrics
            var specificDimension = new Dictionary<string, string>
            {
                ["testDimName1"] = "testDimVal1",
                ["testDimName2"] = "testDimVal2",
            };
            metricSender.Gauge("testmetric", 100, specificDimension);
        }
    }
}
