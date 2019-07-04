//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;

namespace Microsoft.Liftr.Hosting
{
    /// <summary>
    /// Filter the keys to load by prefix and transform the key name.
    /// </summary>
    /// <example>"prefix-Logging--LogLevel--Default" : "Warning"</example>
    internal class PrefixKeyVaultSecretManager : IKeyVaultSecretManager
    {
        private readonly string _prefix;

        public PrefixKeyVaultSecretManager(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            _prefix = $"{prefix}-";
        }

        public bool Load(SecretItem secret)
        {
            // Load a vault secret when its secret name starts with the
            // prefix. Other secrets won't be loaded.
            return secret.Identifier.Name.StartsWith(_prefix, StringComparison.InvariantCultureIgnoreCase);
        }

        public string GetKey(SecretBundle secret)
        {
            // Remove the prefix from the secret name and replace two
            // dashes in any name with the KeyDelimiter, which is the
            // delimiter used in configuration (usually a colon ':'). Azure
            // Key Vault doesn't allow a colon (':') in secret names.
            return secret.SecretIdentifier.Name
                .Substring(_prefix.Length)
                .Replace("--", ConfigurationPath.KeyDelimiter);
        }
    }
}
