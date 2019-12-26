//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Liftr
{
    public static class DateTimeExtensions
    {
        public static string ToZuluString(this DateTime value)
            => value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

        public static string ToZuluString(this DateTimeOffset value)
            => value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);

        public static string ToDateString(this DateTime value)
            => value.ToUniversalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        public static DateTime ParseZuluDateTime(this string value)
        {
            if (value?.OrdinalEndsWith("Z") != true)
            {
                throw new FormatException("DataTime string should be in Zulu format (End with 'Z').");
            }

            return DateTime.Parse(value, CultureInfo.InvariantCulture).ToUniversalTime();
        }

        public static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) =>
            first <= second ? first : second;

        public static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second) =>
            first > second ? first : second;
    }
}
