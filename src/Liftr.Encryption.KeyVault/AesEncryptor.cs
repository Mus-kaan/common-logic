//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault.Core;
using Microsoft.Liftr.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption
{
    public class AesEncryptor : IEncryptor
    {
        private readonly IKey _encryptionKey;
        private readonly string _keyIdentifier;
        private readonly EncryptionAlgorithm _encryptionAlgorithm;

        public AesEncryptor(IKey encryptionKey, string keyIdentifier, EncryptionAlgorithm encryptionAlgorithm)
        {
            if (string.IsNullOrEmpty(keyIdentifier))
            {
                throw new ArgumentNullException(nameof(keyIdentifier));
            }

            _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));

            _keyIdentifier = keyIdentifier;

            _encryptionAlgorithm = encryptionAlgorithm;
        }

        public async Task<(string, IEncryptionMetaData)> EncryptAsync(string content, CancellationToken cancellationToken = default)
        {
            var encryptionRes = await KeyVaultEncryptionUtility.EncryptAsync(
                content,
                _encryptionKey,
                _encryptionAlgorithm,
                cancellationToken);

            EncryptionMetaData encryptionMetaData = new EncryptionMetaData
            {
                EncryptionAlgorithm = _encryptionAlgorithm,
                ContentEncryptionIV = encryptionRes.Item2,
                EncryptionKeyResourceId = _keyIdentifier,
            };

            return (encryptionRes.Item1, encryptionMetaData);
        }
    }
}