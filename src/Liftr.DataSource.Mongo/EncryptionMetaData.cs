//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class EncryptionMetaData : IEncryptionMetaData
    {
        public string ContentEncryptionIV { get; set; }

        public string EncryptionAlgorithm { get; set; }

        public DateTime EncryptionTime { get; set; }

        public string KeyResourceId { get; set; }
    }
}