//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public interface IEncryptionMetaData
    {
        /// <summary>
        /// Resource id of the key used for encryption
        /// </summary>
        public string EncryptionKeyResourceId { get; set; }

        /// <summary>
        /// Encryption algoritm
        /// </summary>
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Encryption initialization vector
        /// </summary>
        public byte[] ContentEncryptionIV { get; set; }
    }
}
