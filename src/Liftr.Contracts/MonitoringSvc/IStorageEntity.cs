//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represents the storage configured for storing intermediate shoe box logs for LogForwarder.
    /// </summary>
    public interface IStorageEntity
    {
        public string DocumentObjectId { get; set; }

        /// <summary>
        /// Name of the storage account.
        /// </summary>
        public string AccountName { get; }

        /// <summary>
        /// Resource Id of the storage account
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        /// Serving location for LogForwarder.
        /// This can be different from the storage account location.
        /// </summary>
        string LogForwarderRegion { get; }

        /// <summary>
        /// Storage account region.
        /// </summary>
        string StorageRegion { get; }

        /// <summary>
        /// Priority of the storage.
        /// </summary>
        StoragePriority Priority { get; }

        /// <summary>
        /// Type of the VNet restrictions.
        /// </summary>
        StorageVNetType VNetType { get; }

        /// <summary>
        /// The last modified timestamp in UTC
        /// </summary>
        DateTime CreatedAtUTC { get; set; }

        /// <summary>
        /// If this storage is ingestionEnabled
        /// </summary>
        bool IngestionEnabled { get; set; }

        /// <summary>
        /// If this storage is active
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        /// Version.
        /// </summary>
        string Version { get; set; }
    }
}
