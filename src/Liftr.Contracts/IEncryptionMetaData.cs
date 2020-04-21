//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts
{
    public interface IEncryptionMetaData
    {
        /// <summary>
        /// The resource Id of the key/securet in KeyVault
        /// </summary>
        public string KeyResourceId { get; set; }

        /// <summary>
        /// Encryption algoritm
        /// </summary>
        public string EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Initialization vector
        /// </summary>
        public string ContentEncryptionIV { get; set; }

        /// <summary>
        /// Encryption date
        /// </summary>
        public DateTime EncryptionTime { get; set; }
    }
}
