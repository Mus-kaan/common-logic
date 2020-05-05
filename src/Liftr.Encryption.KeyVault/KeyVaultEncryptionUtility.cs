//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Core;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Liftr.Contracts;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption
{
    public static class KeyVaultEncryptionUtility
    {
        public const int InitializationVectorLength = 16;
        private const int _refreshTime = 7;
        private static RNGCryptoServiceProvider s_randomNumberGenerator = new RNGCryptoServiceProvider();

        public static async Task<(string, byte[])> EncryptAsync(string inputStr, IKey key, EncryptionAlgorithm encryptionAlgorithm, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var input = string.IsNullOrEmpty(inputStr) ? throw new ArgumentNullException(nameof(inputStr)) : Encoding.UTF8.GetBytes(inputStr);

            var iv = GenerateInitializationVector();

            string algo = encryptionAlgorithm.ToString();

            var result = await key.EncryptAsync(input, iv, iv, algo, cancellationToken);

            var encryptedContent = result.Item1;

            return (Convert.ToBase64String(encryptedContent), iv);
        }

        public static async Task<string> DecryptAsync(string encryptedStr, IKey key, byte[] iv, EncryptionAlgorithm encryptionAlgorithm, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv == null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            var encryptedContent = string.IsNullOrEmpty(encryptedStr) ? throw new ArgumentNullException(nameof(encryptedStr)) : Convert.FromBase64String(encryptedStr);

            string algo = encryptionAlgorithm.ToString();

            var decryptedContent = await key.DecryptAsync(encryptedContent, iv, null, null, algo, cancellationToken);

            return Encoding.UTF8.GetString(decryptedContent, 0, decryptedContent.Length);
        }

        public static async Task<AesSymmetricKey> SetUpKeyVaultSecretAsync(
            IKeyVaultClient keyVaultClient,
            Uri vaultUri,
            string secretName,
            EncryptionAlgorithm encryptionAlgorithm)
        {
            if (keyVaultClient == null)
            {
                throw new ArgumentNullException(nameof(keyVaultClient));
            }

            if (vaultUri == null)
            {
                throw new ArgumentNullException(nameof(vaultUri));
            }

            SecretBundle kvKey = null;
            try
            {
                kvKey = await keyVaultClient.GetSecretAsync($"{vaultUri}secrets/{secretName}");
            }
            catch (KeyVaultErrorException ex)
            {
                if (ex.Response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            if (kvKey == null || (kvKey != null && kvKey.Attributes.Created < DateTime.Today.AddDays(-_refreshTime)))
            {
                return await CreateNewKeyAsync(encryptionAlgorithm, keyVaultClient, vaultUri, secretName);
            }

            AesSymmetricKey aesSymmetricKey = new AesSymmetricKey(
                kvKey.SecretIdentifier.Identifier,
                kvKey.Id,
                Convert.FromBase64String(kvKey.Value));

            return aesSymmetricKey;
        }

        /// <summary>
        /// Generates initialization vector.
        /// </summary>
        /// <returns>Initialization vector.</returns>
        private static byte[] GenerateInitializationVector()
        {
            var iv = new byte[InitializationVectorLength];
            s_randomNumberGenerator.GetBytes(iv);
            return iv;
        }

        private static async Task<AesSymmetricKey> CreateNewKeyAsync(
            EncryptionAlgorithm encryptionAlgorithm,
            IKeyVaultClient keyVaultClient,
            Uri vaultUri,
            string secretName)
        {
            int KeySizeInBits;
            switch (encryptionAlgorithm)
            {
                case EncryptionAlgorithm.A256CBC:
                    KeySizeInBits = 256;
                    break;
                default:
                    throw new InvalidOperationException($"Encryption algorith : {encryptionAlgorithm} is not suported");
            }

            using AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider
            {
                KeySize = KeySizeInBits,
            };

            var key = Convert.ToBase64String(aesCryptoServiceProvider.Key);

            // Store the Base64 of the key in the key vault. Note that the content-type of the secret must
            // be application/octet-stream or the KeyVaultKeyResolver will not load it as a key.
            var KvKey = await keyVaultClient.SetSecretAsync(vaultUri.ToString(), secretName, key, null, "application/octet-stream");

            AesSymmetricKey aesSymmetricKey = new AesSymmetricKey(
                KvKey.SecretIdentifier.Identifier,
                KvKey.Id,
                aesCryptoServiceProvider.Key);

            return aesSymmetricKey;
        }
    }
}
