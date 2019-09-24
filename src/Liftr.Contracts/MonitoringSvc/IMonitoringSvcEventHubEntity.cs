//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represents Eventhub configured for log forwarding realtime intermeadiator
    /// </summary>
    public interface IMonitoringSvcEventHubEntity
    {
        /// <summary>
        /// Partner service type i.e datadog, logz etc
        /// </summary>
        MonitoringSvcType PartnerServiceType { get; }

        /// <summary>
        /// Target datatype , i.e. log. metrics
        /// </summary>
        MonitoringSvcDataType DataType { get; set; }

        /// <summary>
        /// Resource provider type i.e. Microsoft.Datadog/datadogs
        /// </summary>
        string MonitoringSvcResourceProviderType { get; set; }

        /// <summary>
        /// Event hub namespace
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// Event hub name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Event hub location
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Event hub connectionstring
        /// </summary>
        string EventHubConnStr { get; }

        /// <summary>
        /// Storage connectionstring
        /// </summary>
        string StorageConnStr { get; }

        /// <summary>
        /// Event hub authorization rule id
        /// </summary>
        string AuthorizationRuleId { get; }

        /// <summary>
        /// Boolean to represent if event hub is enabled or not
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// The last modified timestamp in UTC
        /// </summary>
        DateTimeOffset TimestampUTC { get; set; }
    }
}
