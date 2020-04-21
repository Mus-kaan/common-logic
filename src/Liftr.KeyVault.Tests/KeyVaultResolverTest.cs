//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.KeyVault;
using System;
using System.Threading;
using Xunit;

namespace Liftr.KeyVault.Tests
{
    public class KeyVaultResolverTest
    {
        [Fact(Skip = "Need to set up a test keyvault")]
        public void GetKeyInString()
        {
            string originalStr = "This is a simple test string";
            string clientId = "clientId";
            string clientSecret = "clinetSecret";

            Convert.FromBase64String(clientSecret);
            string keyIdentifier = "https://logforwarder-df-ms-kv-eu.vault.azure.net/secrets/testsecret/caa08130997946269a432905a7ee5697";
            var kvResolver = new KeyVaultResolver(clientId, clientSecret);
            var key = kvResolver.ResolveSecretAsSymmetricKeyAsync(keyIdentifier, CancellationToken.None).GetAwaiter().GetResult();
            var encrypted = KeyVaultEncryptionHelper.EncryptAsync(originalStr, key).GetAwaiter().GetResult();
            var decrypted = KeyVaultEncryptionHelper.DecryptAsync(encrypted, key).GetAwaiter().GetResult();
            Assert.Equal(originalStr, decrypted);
        }
    }
}
