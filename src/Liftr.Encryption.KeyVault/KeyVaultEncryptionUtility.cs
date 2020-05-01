//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Core;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption.KeyVault
{
    public static class KeyVaultEncryptionUtility
    {
        public const int KeySizeInBits = 128;

        public static async Task<string> EncryptAsync(string inputStr, IKey key, string ivStr, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ivStr == null)
            {
                throw new ArgumentNullException(nameof(ivStr));
            }

            var input = string.IsNullOrEmpty(inputStr) ? throw new ArgumentNullException(nameof(inputStr)) : Encoding.UTF8.GetBytes(inputStr);

            var iv = Convert.FromBase64String(ivStr);

            var result = await key.EncryptAsync(input, iv, null, null, cancellationToken);

            var encryptedContent = result.Item1;

            return Convert.ToBase64String(encryptedContent);
        }

        public static async Task<string> DecryptAsync(string encryptedStr, IKey key, string ivStr, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrEmpty(ivStr))
            {
                throw new ArgumentNullException(nameof(ivStr));
            }

            var encryptedContent = string.IsNullOrEmpty(encryptedStr) ? throw new ArgumentNullException(nameof(encryptedStr)) : Convert.FromBase64String(encryptedStr);

            var iv = Convert.FromBase64String(ivStr);

            var decryptedContent = await key.DecryptAsync(encryptedContent, iv, null, null, null, cancellationToken);

            return Encoding.UTF8.GetString(decryptedContent, 0, decryptedContent.Length);
        }

        public static async Task<AesSymmetricKey> SetUpKeyVaultSecretAsync(
            KeyVaultClient cloudVault,
            string vault,
            string secretName)
        {
            using AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider
            {
                KeySize = KeySizeInBits,
                Mode = CipherMode.CBC,
            };

            var key = Convert.ToBase64String(aesCryptoServiceProvider.Key);
            var iv = Convert.ToBase64String(aesCryptoServiceProvider.IV);

            // Store the Base64 of the key in the key vault. Note that the content-type of the secret must
            // be application/octet-stream or the KeyVaultKeyResolver will not load it as a key.
            var KvKey = await cloudVault.SetSecretAsync(vault, secretName, key, null, "application/octet-stream");

            AesSymmetricKey aesSymmetricKey = new AesSymmetricKey(
                iv,
                KvKey.SecretIdentifier.Identifier,
                KvKey.Id,
                aesCryptoServiceProvider.Key);

            return aesSymmetricKey;
        }
    }
}
