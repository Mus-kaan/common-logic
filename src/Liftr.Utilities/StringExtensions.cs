//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Text;

namespace Microsoft.Liftr
{
    public static class StringExtensions
    {
        public static bool StrictEquals(this string self, string input)
            => self.Equals(input, StringComparison.Ordinal);

        public static bool OrdinalEquals(this string self, string input)
            => self.Equals(input, StringComparison.OrdinalIgnoreCase);

        public static bool OrdinalEndsWith(this string self, string value)
            => self.EndsWith(value, StringComparison.OrdinalIgnoreCase);

        public static int OrdinalIndexOf(this string self, string value)
            => self.IndexOf(value, StringComparison.OrdinalIgnoreCase);

        public static string ToBase64(this string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string input)
        {
            var bytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
