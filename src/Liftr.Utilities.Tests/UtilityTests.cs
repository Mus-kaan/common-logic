//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Data.Common;
using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class UtilityTests
    {
        private const string c_exceptedSerilized = "{\"strProp\":\"string value a.\",\"intProp\":-1222,\"timeStampInZulu\":\"2019-07-05T15:50:54.1793804Z\",\"timeSpanValue\":\"PT55M20.4S\",\"enumValue1\":\"TestEnumValue1\",\"enumValue2\":\"SkuPremium\",\"enumValue3\":\"ALLCAP\",\"enumValue4\":\"Snake_Value\",\"enumValue5\":\"Basic\",\"enumValue6\":\"PremiumSKU\"}";

        [Theory]
        [InlineData("aaa", "aaa")]
        [InlineData("aAa", "aaa")]
        [InlineData("qwert", "qWErt")]
        public void OrdinalEqual(string a, string b)
        {
            Assert.True(a.OrdinalEquals(b));
        }

        [Theory]
        [InlineData("aba", "aaa")]
        [InlineData("aAa", "aba")]
        [InlineData("qwert", "qWErq")]
        public void OrdinalNotEqual(string a, string b)
        {
            Assert.False(a.OrdinalEquals(b));
        }

        [Theory]
        [InlineData("https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test", "aHR0cHM6Ly9kb2NzLm1pY3Jvc29mdC5jb20vZW4tdXMvZG90bmV0L2NvcmUvdGVzdGluZy91bml0LXRlc3Rpbmctd2l0aC1kb3RuZXQtdGVzdA==")]
        [InlineData("Now that you've made one test pass, it's time to write more.", "Tm93IHRoYXQgeW91J3ZlIG1hZGUgb25lIHRlc3QgcGFzcywgaXQncyB0aW1lIHRvIHdyaXRlIG1vcmUu")]
        [InlineData("asdasfaffdgfdeg", "YXNkYXNmYWZmZGdmZGVn")]
        public void Base64EncodeAndDecode(string original, string exceptedEncoding)
        {
            var encoded = original.ToBase64();
            Assert.Equal(exceptedEncoding, encoded);
            var recovered = encoded.FromBase64();
            Assert.Equal(original, recovered);
        }

        [Theory]
        [InlineData("2019-07-05T15:50:54.1793804Z")]
        [InlineData("2019-07-05T15:53:38.4566118Z")]
        [InlineData("2029-07-05T15:50:54.1793804Z")]
        public void DateTimeZulu(string timeString)
        {
            var parsed = timeString.ParseZuluDateTime();
            Assert.Equal(DateTimeKind.Utc, parsed.Kind);
            Assert.Equal(timeString, parsed.ToZuluString());
        }

        [Theory]
        [InlineData("2019-07-05T15:50:54.1793804")]
        [InlineData("asd2029-07-05T15:50:54.1793804Z")]
        public void ParseZuluStringWillThrow(string timeString)
        {
            Assert.Throws<FormatException>(() =>
            {
                timeString.ParseZuluDateTime();
            });
        }

        [Fact]
        public void JsonConverterTest()
        {
            var obj = new TestClass();
            var serialized = obj.ToJson();
            Assert.Equal(c_exceptedSerilized, serialized);
        }
    }
}
