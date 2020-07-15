//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Encryption;
using Microsoft.Liftr.KeyVault;
using Serilog.Core;
using System;
using System.Threading;
using Xunit;

namespace Liftr.Encryption.KeyVault.Tests
{
    public class KeyVaultResolverTest
    {
        private readonly string _vaultUrl = "https://logforwarder-df-ms-kv-eu.vault.azure.net/";
        private readonly string _keyName = "TestEncryptionKey1";
        private readonly string _clientId = "_clientId";
        private readonly string _testContent = "testContent";

        [Fact(Skip = "Need to set up a test keyvault")]
        public void EncyptionAndDecryption()
        {
            string originalStr = "This is a simple test string";
            Uri vaultUri = new Uri(_vaultUrl);

            // Encryption
            KeyVaultClient vaultClient = KeyVaultClientFactory.FromClientIdAndSecret(_clientId, _testContent);
            var key = KeyVaultEncryptionUtility.SetUpKeyVaultSecretAsync(vaultClient, vaultUri, _keyName, EncryptionAlgorithm.A256CBC).GetAwaiter().GetResult();
            var encryptedResult = KeyVaultEncryptionUtility.EncryptAsync(originalStr, key, EncryptionAlgorithm.A256CBC, CancellationToken.None).GetAwaiter().GetResult();
            var encreptedContent = encryptedResult.Item1;
            var iv = encryptedResult.Item2;

            // Decryption
            KeyVaultKeyResolver keyVaultKeyResolver = KeyVaultKeyResolverFactory.FromClientIdAndSecret(_clientId, _testContent);
            var kvResolver = new KeyVaultResolver(keyVaultKeyResolver, Logger.None);
            var keyFromKeyVault = kvResolver.ResolveKeyAsSymmetricKeyAsync(key.KeyIdentifier, CancellationToken.None).GetAwaiter().GetResult();
            var decrypted = KeyVaultEncryptionUtility.DecryptAsync(encreptedContent, keyFromKeyVault, iv, EncryptionAlgorithm.A256CBC, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(originalStr, decrypted);
        }

        [Fact(Skip = "Need to set up a test keyvault")]
        public void EncryptionByEncyptor()
        {
            string originalStr = "This is a simple test string";
            Uri vaultUri = new Uri(_vaultUrl);

            // Encryption
            KeyVaultClient vaultClient = KeyVaultClientFactory.FromClientIdAndSecret(_clientId, _testContent);
            var key = KeyVaultEncryptionUtility.SetUpKeyVaultSecretAsync(vaultClient, vaultUri, _keyName, EncryptionAlgorithm.A256CBC).GetAwaiter().GetResult();
            var encryptor = new AesEncryptor(key, key.KeyIdentifier, EncryptionAlgorithm.A256CBC);
            var res = encryptor.EncryptAsync(originalStr, CancellationToken.None).GetAwaiter().GetResult();

            // Decryption
            KeyVaultKeyResolver keyVaultKeyResolver = KeyVaultKeyResolverFactory.FromClientIdAndSecret(_clientId, _testContent);
            var kvResolver = new KeyVaultResolver(keyVaultKeyResolver, Logger.None);
            var keyFromKeyVault = kvResolver.ResolveKeyAsSymmetricKeyAsync(key.KeyIdentifier, CancellationToken.None).GetAwaiter().GetResult();
            var decrypted = KeyVaultEncryptionUtility.DecryptAsync(res.Item1, keyFromKeyVault, res.Item2.ContentEncryptionIV, res.Item2.EncryptionAlgorithm, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(originalStr, decrypted);
        }

        [Fact(Skip = "Need to set up a test keyvault")]
        public void GenerateRandomIV()
        {
            string originalStr = "This is a simple test string";
            Uri vaultUri = new Uri(_vaultUrl);

            // Encryption
            KeyVaultClient vaultClient = KeyVaultClientFactory.FromClientIdAndSecret(_clientId, _testContent);
            var key = KeyVaultEncryptionUtility.SetUpKeyVaultSecretAsync(vaultClient, vaultUri, _keyName, EncryptionAlgorithm.A256CBC).GetAwaiter().GetResult();
            var encryptor = new AesEncryptor(key, key.KeyIdentifier, EncryptionAlgorithm.A256CBC);
            var res1 = encryptor.EncryptAsync(originalStr, CancellationToken.None).GetAwaiter().GetResult();
            var res2 = encryptor.EncryptAsync(originalStr, CancellationToken.None).GetAwaiter().GetResult();
            Assert.NotEqual(res1.Item2.ContentEncryptionIV, res2.Item2.ContentEncryptionIV);
        }
    }
}
