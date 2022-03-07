//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AzureAd.Icm.Types;
using Microsoft.Liftr.IcmConnector;
using Microsoft.Liftr.Tests;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Prom2IcM.Tests
{
    public class GrafanaIncidentMessageGeneratorTests : LiftrTestBase
    {
        public GrafanaIncidentMessageGeneratorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void VerifyFiring()
        {
            var icmOptions = new ICMClientOptions()
            {
                NotificationEmail = "fake-email@asd.com",
                IcmRoutingId = Guid.NewGuid().ToString(),
            };

            var resolvingWebhook = "{\"title\":\"[OK] Production integration test failure alert\",\"ruleId\":4,\"ruleName\":\"Production integration test failure alert\",\"state\":\"ok\",\"evalMatches\":[],\"orgId\":1,\"dashboardId\":9,\"panelId\":2,\"tags\":{},\"ruleUrl\":\"https://dashboard.wus2.observability-ms.azgrafana-test.io/d/test-integration/test-integration?tab=alert\u0026viewPanel=2\u0026orgId=1\",\"imageUrl\":\"https://stgmdevda211225wus2x.blob.core.windows.net/img-renderer-container/yu1zfiIwBSlISS5M06VJ9OB129S7CT.png\",\"message\":\"[sev3]  There are at least 2 failed integration test run in the past hour. Please see details in Jenkins: \nhttps://jenkins.cicd.azliftr-test.io/\"}";
            var grafanaWebhookMessage = resolvingWebhook.FromJson<GrafanaWebhookMessage>();

            var generated = GrafanaIncidentMessageGenerator.GenerateIncidentFromGrafanaAlert(grafanaWebhookMessage, icmOptions, Logger);
            Assert.Equal("Production integration test failure alert", generated.Title);
            Assert.Equal(IncidentStatus.Active, generated.Status);
        }

        [Theory]
        [InlineData("[OK] Production integration test failure alert", "Production integration test failure alert")]
        [InlineData("[Alerting] Production integration test failure alert", "Production integration test failure alert")]
        [InlineData("Production integration test failure alert", "Production integration test failure alert")]
        [InlineData("Product[ion integratio]n test failure alert", "Product[ion integratio]n test failure alert")]
        public void VerifyNormalizeTitle(string message, string parsedTitle)
        {
            var output = GrafanaIncidentMessageGenerator.NormalizeGrafanaAlertTitle(message);
            Assert.Equal(parsedTitle, output);
        }

        [Fact]
        public void VerifyParseSeverity()
        {
            var message = "[Severity 3] other message";
            var parsed = GrafanaIncidentMessageGenerator.ExtractSeverityFromMessage(message);
            Assert.Equal(3, parsed);
        }

        [Theory]
        [InlineData("[Severity3] other message", 3)]
        [InlineData("[Severity 3] other message", 3)]
        [InlineData("[Sev 3] other message", 3)]
        [InlineData("[Sev3] other message", 3)]
        [InlineData("[severity3] other message", 3)]
        [InlineData("[severity 3] other message", 3)]
        [InlineData("[sev 3] other message", 3)]
        [InlineData("[sev3] other message", 3)]
        [InlineData("asdas [Severity3] other message", 3)]
        [InlineData("fdg45 [Severity 3] other message", 3)]
        [InlineData("wef34 [Sev 3] other message", 3)]
        [InlineData("kii7 [Sev3] other message", 3)]
        public void CanParseSeverityFromMessage(string message, int parsedSeverity)
        {
            var parsed = GrafanaIncidentMessageGenerator.ExtractSeverityFromMessage(message);
            Assert.Equal(parsedSeverity, parsed);
        }

        [Theory]
        [InlineData("asdasd other message", 4)]
        [InlineData("[S3evserity3] other message", 4)]
        [InlineData("[Seve2rity3] other message", 4)]
        [InlineData("[Se2verity 3] other message", 4)]
        [InlineData("[Se2v 3] other message", 4)]
        [InlineData("[Sev53] other message", 4)]
        [InlineData("[severity31] other message", 4)]
        [InlineData("[severity -13] other message", 4)]
        [InlineData("[se2v 3] other message", 4)]
        [InlineData("[se3v3] other message", 4)]
        [InlineData("asdas [Severity53] other message", 4)]
        [InlineData("fdg45 [Severity -3] other message", 4)]
        [InlineData("wef34 [Sev 63] other message", 4)]
        [InlineData("kii7 [S2ev3] other message", 4)]
        public void WillIgnoreInvalidSeverityFromMessage(string message, int parsedSeverity)
        {
            var parsed = GrafanaIncidentMessageGenerator.ExtractSeverityFromMessage(message);
            Assert.Equal(parsedSeverity, parsed);
        }
    }
}
