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
            => string.Equals(self, input, StringComparison.Ordinal);

        public static bool OrdinalEquals(this string self, string input)
            => string.Equals(self, input, StringComparison.OrdinalIgnoreCase);

        public static bool OrdinalContains(this string self, string value)
        {
            Ensure.ArgumentNotNull(self, nameof(self));

            return self.OrdinalIndexOf(value) != -1;
        }

        public static bool OrdinalEndsWith(this string self, string value)
        {
            Ensure.ArgumentNotNull(self, nameof(self));

            return self.EndsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        public static int OrdinalIndexOf(this string self, string value)
        {
            Ensure.ArgumentNotNull(self, nameof(self));

            return self.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

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
