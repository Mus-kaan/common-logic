//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class ImageBuilderOrchestratorTests
    {
        [Theory]
        [InlineData("18.04_Ni-1", "U1804LTS_Ni-1")]
        [InlineData("18.04_Ni-4-FIPS", "U1804FIPS_Ni-4")]
        public void VerifyParseversionTagFromLabel(string expected, string input)
        {
            var parsed = ImageBuilderOrchestrator.ParseSBIVersionTag(input);
            Assert.Equal(expected, parsed);
        }
    }
}
