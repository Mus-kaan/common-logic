//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class AddLFStorageMessage
    {
        /// <summary>
        /// Name of the storage account.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Resource Id of the storage account
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Serving location for LogForwarder.
        /// This can be different from the storage account location.
        /// </summary>
        public string LogForwarderRegion { get; set; }

        /// <summary>
        /// Storage account region.
        /// </summary>
        public string StorageRegion { get; set; }

        /// <summary>
        /// Priority of the storage.
        /// </summary>
        public StoragePriority Priority { get; set; }
    }
}
