//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption
{
    /// <summary>
    /// Decryptor
    /// </summary>
    public interface IDecryptor
    {
        public Task<string> DecryptAsync(IEncryptionMetaData encryptionMetaData, string encryptedStr, CancellationToken cancellationToken = default);
    }
}