//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represent each monitoredresource with its partner properties
    /// </summary>
    public interface IMonitoringSvcMonitoredEntity
    {
        /// <summary>
        /// Target monitored resouce id by monitoring resource
        /// eg => MonitoringResource = Microsoft.Datadog/Logz
        ///    => MonitoredResource = All azure monitorable resources
        /// </summary>
        string MonitoredResourceId { get; }

        /// <summary>
        /// Source monitoring resource id like datadog/logz resource id
        /// </summary>
        string MonitoringResourceId { get; }

        /// <summary>
        /// MonitoredResource's resource type
        /// </summary>
        string ResourceType { get; }

        /// <summary>
        /// MonitoringResource's partner service type i.e. Datadog/Logz
        /// </summary>
        MonitoringSvcType PartnerServiceType { get; }

        /// <summary>
        /// Partner credential to forward logs to partner service
        /// i.e. in case of datadog, it is api key
        /// </summary>
        string PartnerCredential { get; }

        /// <summary>
        /// Priority for log/metric entry
        /// </summary>
        uint Priority { get; }

        /// <summary>
        /// Boolen value to represent if current monitored resource is enabled or not
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// The last modified timestamp in UTC
        /// </summary>
        DateTimeOffset TimestampUTC { get; set; }
    }
}
