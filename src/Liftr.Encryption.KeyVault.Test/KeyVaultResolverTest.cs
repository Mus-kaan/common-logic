//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.Encryption.KeyVault;
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
        private readonly string _keyName = "TestEncryptionKey";
        private readonly string _clientId = "_clientId";
        private readonly string _clientSecret = "_clientSecret";

        [Fact(Skip = "Need to set up a test keyvault")]
        public void EncyptionAndDecryption()
        {
            string originalStr = "This is a simple test string";

            // Encryption
            KeyVaultClient vaultClient = KeyVaultClientFactory.FromClientIdAndSecret(_clientId, _clientSecret);
            var key = KeyVaultEncryptionUtility.SetUpKeyVaultSecretAsync(vaultClient, _vaultUrl, _keyName).GetAwaiter().GetResult();
            var encryptedStr = KeyVaultEncryptionUtility.EncryptAsync(originalStr, key, key.InitializationVector, CancellationToken.None).GetAwaiter().GetResult();

            // Decryption
            KeyVaultKeyResolver keyVaultKeyResolver = KeyVaultKeyResolverFactory.FromClientIdAndSecret(_clientId, _clientSecret);
            var kvResolver = new KeyVaultResolver(keyVaultKeyResolver, Logger.None);
            var keyFromKeyVault = kvResolver.ResolveKeyAsSymmetricKeyAsync(key.KeyIdentifier, CancellationToken.None).GetAwaiter().GetResult();
            var decrypted = KeyVaultEncryptionUtility.DecryptAsync(encryptedStr, keyFromKeyVault, key.InitializationVector, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(originalStr, decrypted);
        }

        [Fact(Skip = "Need to set up a test keyvault")]
        public void DecryptValueWithOldKey()
        {
            string originalStr = "This is a simple test string";

            // Encryption
            KeyVaultClient vaultClient = KeyVaultClientFactory.FromClientIdAndSecret(_clientId, _clientSecret);
            var key = KeyVaultEncryptionUtility.SetUpKeyVaultSecretAsync(vaultClient, _vaultUrl, _keyName).GetAwaiter().GetResult();
            var encryptedStr = KeyVaultEncryptionUtility.EncryptAsync(originalStr, key, key.InitializationVector, CancellationToken.None).GetAwaiter().GetResult();

            // New key generated
            KeyVaultEncryptionUtility.SetUpKeyVaultSecretAsync(vaultClient, _vaultUrl, _keyName).GetAwaiter().GetResult();

            // Decryption
            KeyVaultKeyResolver keyVaultKeyResolver = KeyVaultKeyResolverFactory.FromClientIdAndSecret(_clientId, _clientSecret);
            var kvResolver = new KeyVaultResolver(keyVaultKeyResolver, Logger.None);
            var keyFromKeyVault = kvResolver.ResolveKeyAsSymmetricKeyAsync(key.KeyIdentifier, CancellationToken.None).GetAwaiter().GetResult();
            var decrypted = KeyVaultEncryptionUtility.DecryptAsync(encryptedStr, keyFromKeyVault, key.InitializationVector, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(originalStr, decrypted);
        }
    }
}
