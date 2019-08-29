//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Globalization;
using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class ZuluDateTimeConverterTests
    {
        [Fact]
        public void JsonConverterTest()
        {
            var obj = new DateTimeClass();
            var serialized = JsonConvert.SerializeObject(obj);
            var recovered = JsonConvert.DeserializeObject<DateTimeClass>(serialized);
            Assert.True(serialized.IndexOf("Z", StringComparison.OrdinalIgnoreCase) > 0);
            Assert.Equal(DateTimeKind.Unspecified, recovered.Time1.Kind);
            Assert.Equal(DateTimeKind.Utc, recovered.Time2.Kind);
        }
    }

    public class DateTimeClass
    {
        public DateTime Time1 { get; set; } = GetTime();

        [JsonConverter(typeof(ZuluDateTimeConverter))]
        public DateTime Time2 { get; set; } = GetTime();

        private static DateTime GetTime()
        {
            return DateTime.Parse("2019-09-28", CultureInfo.InvariantCulture);
        }
    }
}
