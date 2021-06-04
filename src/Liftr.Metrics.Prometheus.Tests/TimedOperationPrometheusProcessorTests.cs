//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Metrics.Prom;
using Xunit;

namespace Microsoft.Liftr.Metrics.Prometheus.Tests
{
    public class TimedOperationPrometheusProcessorTests
    {
        [Theory]
        [InlineData("load_certificate_from_key_vault", "LoadCertificateFromKeyVault")]
        [InlineData("test_sc", "TestSC")]
        [InlineData("test_sc", "testSC")]
        [InlineData("test_snake_case", "TestSnakeCase")]
        [InlineData("test_snake_case", "testSnakeCase")]
        [InlineData("test_snake_case123", "TestSnakeCase123")]
        [InlineData("_test_snake_case123", "_testSnakeCase123")]
        [InlineData("test_sc", "test_SC")]
        public void OrdinalEqual(string expected, string input)
        {
            var metricsName = TimedOperationPrometheusProcessor.ConvertToPrometheusMetricsName(input);
            Assert.Equal(expected, metricsName);
        }
    }
}
