//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represents vm extension configuration for any resource type
    /// </summary>
    public interface IMonitoringSvcVMExtensionDetailsEntity
    {
        /// <summary>
        /// Resource provider type i.e. Microsoft.Datadog
        /// </summary>
        string MonitoringSvcResourceProviderType { get; }

        /// <summary>
        /// Azure vm extension na,e
        /// </summary>
        string ExtensionName { get; set; }

        /// <summary>
        /// Extension publisher name
        /// </summary>
        string PublisherName { get; set; }

        /// <summary>
        /// Azure vm extension type
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Azrue vm extension version
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// Target operating system, Azure vm extensions are operating system specific
        /// </summary>
        string OperatingSystem { get; set; }

        /// <summary>
        /// The last modified timestamp in UTC
        /// </summary>
        DateTimeOffset TimestampUTC { get; set; }
    }
}
