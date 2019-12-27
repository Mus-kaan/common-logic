//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Liftr
{
    public static class GuidUtility
    {
        // Utility to generate deterministic guids from strings
        // http://geekswithblogs.net/EltonStoneman/archive/2008/06/26/generating-deterministic-guids.aspx
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        private static readonly MD5CryptoServiceProvider s_provider = new MD5CryptoServiceProvider();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms

        public static Guid GetDeterministicGuid(string input)
        {
            byte[] inputBytes = Encoding.Default.GetBytes(input);
            byte[] hashBytes = s_provider.ComputeHash(inputBytes);
            Guid hashGuid = new Guid(hashBytes);
            return hashGuid;
        }
    }
}
