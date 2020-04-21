//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.KeyVault
{
    public static class KeyVaultEncryptionHelper
    {
        public static readonly byte[] DefaultInitializationVector = new byte[] { 0x16, 0xA8, 0xFA, 0x0B, 0x1E, 0x07, 0xB5, 0xC1, 0x5D, 0xE7, 0x50, 0xDE, 0x85, 0x2E, 0x62, 0xEA };

        public static async Task<string> EncryptAsync(string inputStr, SymmetricKey key, string ivStr = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var input = string.IsNullOrEmpty(inputStr) ? throw new ArgumentNullException(nameof(inputStr)) : Encoding.UTF8.GetBytes(inputStr);

            var iv = ivStr == null ? DefaultInitializationVector : Encoding.UTF8.GetBytes(ivStr);

            var result = await key.EncryptAsync(input, iv);

            var encryptedContent = result.Item1;

            return Convert.ToBase64String(encryptedContent);
        }

        public static async Task<string> DecryptAsync(string encryptedStr, SymmetricKey key, string ivStr = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var encryptedContent = string.IsNullOrEmpty(encryptedStr) ? throw new ArgumentNullException(nameof(encryptedStr)) : Convert.FromBase64String(encryptedStr);

            var iv = ivStr == null ? DefaultInitializationVector : Encoding.UTF8.GetBytes(ivStr);

            var decryptedContent = await key.DecryptAsync(encryptedContent, iv);

            return Encoding.UTF8.GetString(decryptedContent, 0, decryptedContent.Length);
        }
    }
}
