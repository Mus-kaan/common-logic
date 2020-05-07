//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Initialization vector
        /// </summary>
        public byte[] ContentEncryptionIV { get; set; }
    }
}
