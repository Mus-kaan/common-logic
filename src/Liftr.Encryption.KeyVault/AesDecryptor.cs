//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault.Core;
using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption
{
    /// <summary>
    /// Decryptor
    /// </summary>
    public sealed class AesDecryptor : IDecryptor, IDisposable
    {
        private readonly Dictionary<string, IKey> _encryptionKeys = new Dictionary<string, IKey>();
        private readonly KeyVaultResolver _keyVaultResolver;
        private readonly SemaphoreSlim _mu = new SemaphoreSlim(1, 1);

        public AesDecryptor(KeyVaultResolver keyVaultResolver)
        {
            _keyVaultResolver = keyVaultResolver ?? throw new ArgumentNullException(nameof(keyVaultResolver));
        }

        public void Dispose()
        {
            _mu.Dispose();
        }

        public async Task<string> DecryptAsync(IEncryptionMetaData encryptionMetaData, string encryptedStr, CancellationToken cancellationToken = default)
        {
            if (encryptionMetaData == null)
            {
                throw new ArgumentNullException(nameof(encryptionMetaData));
            }

            IKey key;
            await _mu.WaitAsync();
            try
            {
                if (!_encryptionKeys.ContainsKey(encryptionMetaData.KeyResourceId))
                {
                    _encryptionKeys[encryptionMetaData.KeyResourceId] = await _keyVaultResolver.ResolveKeyAsSymmetricKeyAsync(encryptionMetaData.KeyResourceId, cancellationToken);
                }

                key = _encryptionKeys[encryptionMetaData.KeyResourceId];
            }
            finally
            {
                _mu.Release();
            }

            return await KeyVaultEncryptionUtility.DecryptAsync(
                encryptedStr,
                key,
                encryptionMetaData.ContentEncryptionIV,
                encryptionMetaData.EncryptionAlgorithm,
                cancellationToken);
        }
    }
}
