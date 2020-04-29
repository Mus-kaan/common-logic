//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using System;

namespace Microsoft.Liftr.Encryption.KeyVault
{
    public class AesSymmetricKey : SymmetricKey
    {
        /// <summary>
        /// Creates an AesSymmetricKey
        /// </summary>
        /// <param name="initializationVector">Initialization Vector that will be used to encrypt</param>
        /// <param name="identifier">Resource ID of this encryption key</param>
        /// <param name="kid">Id of the encryption key</param>
        /// <param name="key">The Value of the encryption key</param>
        public AesSymmetricKey(string initializationVector, string identifier, string kid, byte[] key)
            : base(kid, key)
        {
            if (string.IsNullOrEmpty(initializationVector))
            {
                throw new ArgumentNullException(nameof(initializationVector));
            }

            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            InitializationVector = initializationVector;

            KeyIdentifier = identifier;
        }

        /// <summary>
        /// Initialization Vector
        /// </summary>
        public string InitializationVector { get; }

        /// <summary>
        /// Resource Id of the encryption key
        /// </summary>
        public string KeyIdentifier { get; }
    }
}
