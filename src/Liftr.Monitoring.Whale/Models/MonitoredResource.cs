//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Monitoring.Whale.Models
{
    /// <summary>
    /// The properties of a resource currently being monitored by the Monitoring monitor resource.
    /// </summary>
    public class MonitoredResource
    {
        /// <summary>
        /// The ARM id of the resource.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The location of the resource.
        /// </summary>
        public string Location { get; set; }
    }
}
