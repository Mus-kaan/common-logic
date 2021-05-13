//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class AIBClientTests
    {
        private static string s_SampleRunOutput = "ewogICJpZCI6ICIvc3Vic2NyaXB0aW9ucy9kMjFhNTI1ZS03Yzg2LTQ4NmQtYTc5ZS1hNGYzNjIyZjYzOWEvcmVzb3VyY2Vncm91cHMvTEZUZXN0UkcvcHJvdmlkZXJzL01pY3Jvc29mdC5WaXJ0dWFsTWFjaGluZUltYWdlcy9pbWFnZVRlbXBsYXRlcy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDEvcnVuT3V0cHV0cy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDEudmhkIiwKICAiaWRlbnRpdHkiOiBudWxsLAogICJraW5kIjogbnVsbCwKICAibG9jYXRpb24iOiBudWxsLAogICJtYW5hZ2VkQnkiOiBudWxsLAogICJuYW1lIjogIlRlc3RJbWFnZU5hbWUxMjMtMS4yLjYwMS52aGQiLAogICJwbGFuIjogbnVsbCwKICAicHJvcGVydGllcyI6IHsKICAgICJhcnRpZmFjdFVyaSI6ICJodHRwczovL3R3ZjM1ZmpiZHdqeXZoem83MGZoZGhyNi5ibG9iLmNvcmUud2luZG93cy5uZXQvdmhkcy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDFfMjAyMDA1MDYxNjIyMTcudmhkP3NlPTIwMjAtMDYtMDVUMTYlM0EyMiUzQTE5WiZzaWc9cVdiUjVaSGo3MFo3REs5U1NUbm96bUFlOWhqWHcxa2tlR3NFU0w3UHJvYyUzRCZzcD1yJnNwcj1odHRwcyZzcj1iJnN2PTIwMTgtMDMtMjgiLAogICAgInByb3Zpc2lvbmluZ1N0YXRlIjogIlN1Y2NlZWRlZCIKICB9LAogICJyZXNvdXJjZUdyb3VwIjogIkxGVGVzdFJHIiwKICAic2t1IjogbnVsbCwKICAidGFncyI6IG51bGwsCiAgInR5cGUiOiAiTWljcm9zb2Z0LlZpcnR1YWxNYWNoaW5lSW1hZ2VzL2ltYWdlVGVtcGxhdGVzL3J1bk91dHB1dHMiCn0=".FromBase64();
        private static string s_NewSampleRunOutput = "ewogICJpZCI6ICIvc3Vic2NyaXB0aW9ucy9kMjFhNTI1ZS03Yzg2LTQ4NmQtYTc5ZS1hNGYzNjIyZjYzOWEvcmVzb3VyY2Vncm91cHMvTEZUZXN0UkcvcHJvdmlkZXJzL01pY3Jvc29mdC5WaXJ0dWFsTWFjaGluZUltYWdlcy9pbWFnZVRlbXBsYXRlcy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDEvcnVuT3V0cHV0cy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDEudmhkIiwKICAiaWRlbnRpdHkiOiBudWxsLAogICJraW5kIjogbnVsbCwKICAibG9jYXRpb24iOiBudWxsLAogICJtYW5hZ2VkQnkiOiBudWxsLAogICJuYW1lIjogIlRlc3RJbWFnZU5hbWUxMjMtMS4yLjYwMS52aGQiLAogICJwbGFuIjogbnVsbCwKICAicHJvcGVydGllcyI6IHsKICAgICJhcnRpZmFjdFVyaSI6ICJodHRwczovL3R3ZjM1ZmpiZHdqeXZoem83MGZoZGhyNi5ibG9iLmNvcmUud2luZG93cy5uZXQvdmhkcy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDFfMjAyMDA1MDYxNjIyMTcudmhkIiwKICAgICJwcm92aXNpb25pbmdTdGF0ZSI6ICJTdWNjZWVkZWQiCiAgfSwKICAicmVzb3VyY2VHcm91cCI6ICJMRlRlc3RSRyIsCiAgInNrdSI6IG51bGwsCiAgInRhZ3MiOiBudWxsLAogICJ0eXBlIjogIk1pY3Jvc29mdC5WaXJ0dWFsTWFjaGluZUltYWdlcy9pbWFnZVRlbXBsYXRlcy9ydW5PdXRwdXRzIgp9".FromBase64();
        private static string s_vhdUri = "https://twf35fjbdwjyvhzo70fhdhr6.blob.core.windows.net/vhds/TestImageName123-1.2.601_20200506162217.vhd";

        [Fact]
        public void CanParseAIBRunoutput()
        {
            string extractedVHD;
            Assert.True(AIBClient.TryExtractVHDUriFromRunOutput(s_SampleRunOutput, out extractedVHD));
            Assert.Equal(s_vhdUri, extractedVHD);

            Assert.True(AIBClient.TryExtractVHDUriFromRunOutput(s_NewSampleRunOutput, out extractedVHD));
            Assert.Equal(s_vhdUri, extractedVHD);
        }
    }
}
