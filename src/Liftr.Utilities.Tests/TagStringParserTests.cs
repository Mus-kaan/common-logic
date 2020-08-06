//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class TagStringParserTests
    {
        [Fact]
        public void TwoNullEquals()
        {
            if (TagStringParser.TryParse("Department:IT;Environment:Prod;Role:WorkerRole;T1:a;T2:a:1", out var tags))
            {
                Assert.Equal(5, tags.Count);
                Assert.Equal("IT", tags["Department"]);
                Assert.Equal("Prod", tags["Environment"]);
                Assert.Equal("WorkerRole", tags["Role"]);
                Assert.Equal("a", tags["T1"]);
                Assert.Equal("a:1", tags["T2"]);
            }
            else
            {
                Assert.False(true);
            }

            if (TagStringParser.TryParse(null, out tags))
            {
                Assert.Empty(tags);
            }
            else
            {
                Assert.False(true);
            }

            if (TagStringParser.TryParse(string.Empty, out tags))
            {
                Assert.Empty(tags);
            }
            else
            {
                Assert.False(true);
            }

            if (TagStringParser.TryParse("Department:IT", out tags))
            {
                Assert.Single(tags);
                Assert.Equal("IT", tags["Department"]);
            }

            if (TagStringParser.TryParse("aksEngineVersion:v0.47.0-aks-gomod-85-aks;creationSource:aks-aks-spdevwus2-37058798-vmss;orchestrator:Kubernetes:1.17.7;poolName:spdevwus2;resourceNameSuffix:37058798", out tags))
            {
                Assert.Equal(5, tags.Count);
            }
            else
            {
                Assert.False(true);
            }
        }

        [Theory]
        [InlineData("2019-07-05T15:50:54.1793804")]
        [InlineData("asda:asd:")]
        [InlineData("asda:asd;")]
        [InlineData("asda")]
        public void ParseZuluStringWillThrow(string tagString)
        {
            Assert.False(TagStringParser.TryParse(tagString, out _));
        }
    }
}
