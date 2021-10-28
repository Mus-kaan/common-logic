//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.Prom2IcM.Tests
{
    public class GrafanaIncidentMessageGeneratorTests
    {
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
