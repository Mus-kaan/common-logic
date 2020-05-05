//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Encryption
{
    public interface IEncryptor
    {
        public Task<(string, IEncryptionMetaData)> EncryptAsync(string content, CancellationToken cancellationToken = default);
    }
}