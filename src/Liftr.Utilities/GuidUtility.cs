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
        private static readonly SHA256 s_hashGenerator = SHA256.Create();

        public static Guid GetDeterministicGuid(string input)
        {
            byte[] inputBytes = Encoding.Default.GetBytes(input);
            byte[] hashBytes = s_hashGenerator.ComputeHash(inputBytes);
            Guid hashGuid = new Guid(hashBytes);
            return hashGuid;
        }
    }
}
