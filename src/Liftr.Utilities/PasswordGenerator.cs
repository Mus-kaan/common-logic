//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Liftr.Utilities
{
    public static class PasswordGenerator
    {
        public const string lower = "abcdefghijklmnopqrstuvwxyz";
        public const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string number = "1234567890";
        public const string special = "!@$%^&*_-=+";

        private static readonly Random s_rand = new Random();

        /// <summary>
        /// Generate a password
        /// </summary>
        /// <param name="length">length of the password</param>
        /// <param name="includeSpecialCharacter">Include special characters"</param>
        /// <returns></returns>
        public static string Generate(int length = 24, bool includeSpecialCharacter = true)
        {
            // Get cryptographically random sequence of bytes
            var bytes = new byte[length];
            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);

            // Build up a string using random bytes and character classes
            var res = new StringBuilder();
            foreach (byte b in bytes)
            {
                // Randomly select a character class for each byte
                var charSet = lower;
                var modeCount = includeSpecialCharacter ? 4 : 3;
                switch (s_rand.Next(modeCount))
                {
                    // In each case use mod to project byte b to the correct range
                    case 0:
                        charSet = lower;
                        break;
                    case 1:
                        charSet = upper;
                        break;
                    case 2:
                        charSet = number;
                        break;
                    case 3:
                        charSet = special;
                        break;
                }

                res.Append(charSet[b % charSet.Count()]);
            }

            return res.ToString();
        }
    }
}
