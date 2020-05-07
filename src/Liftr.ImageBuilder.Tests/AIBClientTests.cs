//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class AIBClientTests
    {
        private static string s_SampleRunOutput = "ewogICJpZCI6ICIvc3Vic2NyaXB0aW9ucy9kMjFhNTI1ZS03Yzg2LTQ4NmQtYTc5ZS1hNGYzNjIyZjYzOWEvcmVzb3VyY2Vncm91cHMvTEZUZXN0UkcvcHJvdmlkZXJzL01pY3Jvc29mdC5WaXJ0dWFsTWFjaGluZUltYWdlcy9pbWFnZVRlbXBsYXRlcy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDEvcnVuT3V0cHV0cy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDEudmhkIiwKICAiaWRlbnRpdHkiOiBudWxsLAogICJraW5kIjogbnVsbCwKICAibG9jYXRpb24iOiBudWxsLAogICJtYW5hZ2VkQnkiOiBudWxsLAogICJuYW1lIjogIlRlc3RJbWFnZU5hbWUxMjMtMS4yLjYwMS52aGQiLAogICJwbGFuIjogbnVsbCwKICAicHJvcGVydGllcyI6IHsKICAgICJhcnRpZmFjdFVyaSI6ICJodHRwczovL3R3ZjM1ZmpiZHdqeXZoem83MGZoZGhyNi5ibG9iLmNvcmUud2luZG93cy5uZXQvdmhkcy9UZXN0SW1hZ2VOYW1lMTIzLTEuMi42MDFfMjAyMDA1MDYxNjIyMTcudmhkP3NlPTIwMjAtMDYtMDVUMTYlM0EyMiUzQTE5WiZzaWc9cVdiUjVaSGo3MFo3REs5U1NUbm96bUFlOWhqWHcxa2tlR3NFU0w3UHJvYyUzRCZzcD1yJnNwcj1odHRwcyZzcj1iJnN2PTIwMTgtMDMtMjgiLAogICAgInByb3Zpc2lvbmluZ1N0YXRlIjogIlN1Y2NlZWRlZCIKICB9LAogICJyZXNvdXJjZUdyb3VwIjogIkxGVGVzdFJHIiwKICAic2t1IjogbnVsbCwKICAidGFncyI6IG51bGwsCiAgInR5cGUiOiAiTWljcm9zb2Z0LlZpcnR1YWxNYWNoaW5lSW1hZ2VzL2ltYWdlVGVtcGxhdGVzL3J1bk91dHB1dHMiCn0=".FromBase64();
        private static string s_vhdUri = "aHR0cHM6Ly90d2YzNWZqYmR3anl2aHpvNzBmaGRocjYuYmxvYi5jb3JlLndpbmRvd3MubmV0L3ZoZHMvVGVzdEltYWdlTmFtZTEyMy0xLjIuNjAxXzIwMjAwNTA2MTYyMjE3LnZoZD9zZT0yMDIwLTA2LTA1VDE2JTNBMjIlM0ExOVomc2lnPXFXYlI1WkhqNzBaN0RLOVNTVG5vem1BZTloalh3MWtrZUdzRVNMN1Byb2MlM0Qmc3A9ciZzcHI9aHR0cHMmc3I9YiZzdj0yMDE4LTAzLTI4".FromBase64();

        [Fact]
        public void CanParseAIBRunoutput()
        {
            string extractedVHD;
            Assert.True(AIBClient.TryExtractVHDSASFromRunOutput(s_SampleRunOutput, out extractedVHD));
            Assert.Equal(s_vhdUri, extractedVHD);
        }
    }
}
