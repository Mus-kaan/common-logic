//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Core;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption
{
    public class KeyVaultResolver
    {
        private readonly KeyVaultKeyResolver _cloudResolver;
        private readonly Serilog.ILogger _logger;

        public KeyVaultResolver(KeyVaultKeyResolver cloudResolver, ILogger logger)
        {
            _cloudResolver = cloudResolver ?? throw new ArgumentNullException(nameof(cloudResolver));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IKey> ResolveKeyAsSymmetricKeyAsync(string keyIdentifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(keyIdentifier) || !SecretIdentifier.IsSecretIdentifier(keyIdentifier))
            {
                throw new ArgumentNullException(nameof(keyIdentifier));
            }

            IKey symmetricKey = null;
            using (var operation = _logger.StartTimedOperation("Resolving key from KeyVault"))
            {
                symmetricKey = await _cloudResolver.ResolveKeyAsync(keyIdentifier, cancellationToken);
            }

            if (symmetricKey == null)
            {
                throw new InvalidOperationException("Secret is not a valid symmetricKey");
            }

            return symmetricKey;
        }
    }
}
