//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.MonitoringSvc
{
    /// <summary>
    /// This entity represents Eventhub configured for log forwarding realtime intermeadiator
    /// </summary>
    public interface IEventHubEntity
    {
        public string DocumentObjectId { get; set; }

        /// <summary>
        /// Resource provider name i.e. Microsoft.Datadog
        /// </summary>
        MonitoringResourceProvider ResourceProvider { get; set; }

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
        string EventHubConnectionString { get; }

        /// <summary>
        /// Storage connectionstring
        /// </summary>
        string StorageConnectionString { get; }

        /// <summary>
        /// Event hub authorization rule id
        /// </summary>
        string AuthorizationRuleId { get; }

        /// <summary>
        /// The last modified timestamp in UTC
        /// </summary>
        DateTime CreatedAtUTC { get; set; }

        /// <summary>
        /// Should this eventhub be used to set in the Diagnostics settings.
        /// </summary>
        bool IngestionEnabled { get; set; }

        /// <summary>
        /// Should LogForwarder listen and read data from this eventhub.
        /// </summary>
        bool Active { get; set; }
    }
}
