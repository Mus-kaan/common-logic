//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.Encryption
{
    public class EncryptionMetaData : IEncryptionMetaData
    {
        public byte[] ContentEncryptionIV { get; set; }

        public EncryptionAlgorithm EncryptionAlgorithm { get; set; }

        public string KeyResourceId { get; set; }
    }
}