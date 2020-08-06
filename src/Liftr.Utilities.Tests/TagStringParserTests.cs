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
            if (TagStringParser.TryParse("Department:IT;Environment:Prod;Role:WorkerRole", out var tags))
            {
                Assert.Equal(3, tags.Count);
                Assert.Equal("IT", tags["Department"]);
                Assert.Equal("Prod", tags["Environment"]);
                Assert.Equal("WorkerRole", tags["Role"]);
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
