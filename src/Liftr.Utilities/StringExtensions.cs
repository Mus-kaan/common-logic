//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
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

        public static bool OrdinalStartsWith(this string self, string value)
        {
            Ensure.ArgumentNotNull(self, nameof(self));

            return self.StartsWith(value, StringComparison.OrdinalIgnoreCase);
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

        public static bool IsBase64(this string base64String)
        {
            if (string.IsNullOrEmpty(base64String)
                || base64String.Length % 4 != 0
                || base64String.Contains("\"")
                || base64String.Contains(" ")
                || base64String.Contains("\t")
                || base64String.Contains("\r")
                || base64String.Contains("\n"))
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {
                return false;
            }
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

        public static string NormalizedAzRegion(this string input)
            => input?.Replace(" ", string.Empty)?.ToLowerInvariant(); // https://github.com/Azure/azure-libraries-for-net/blob/f5298f4f9c257dcf113b76ac86bcad25f050af8b/src/ResourceManagement/ResourceManager/Region.cs#L135

        public static string RemoveWhitespace(this string input)
        {
            Ensure.ArgumentNotNull(input, nameof(input));

            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        public static int OrdinalSubstringCount(this string text, string pattern)
        {
            Ensure.ArgumentNotNull(text, nameof(text));
            if (string.IsNullOrEmpty(pattern))
            {
                return 0;
            }

            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                i += pattern.Length;
                count++;
            }

            return count;
        }
    }
}
