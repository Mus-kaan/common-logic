//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class UpdateMonitorCreationStatusMessage
    {
        /// <summary>
        /// The resource Id of the monitor resource.
        /// </summary>
        public string MonitorId { get; set; }

        /// <summary>
        /// The monitor creation status object.
        /// </summary>
        public string MonitorCreationStatusObject { get; set; }

        /// <summary>
        /// Option for API token update.
        /// </summary>
        public bool IsUpdateAPIToken { get; set; }

        /// <summary>
        /// Option for shipping token update.
        /// </summary>
        public bool IsUpdateShippingToken { get; set; }

        /// <summary>
        /// Option for marketplace resource Id update.
        /// </summary>
        public bool IsUpdateMarketplaceResourceId { get; set; }
    }
}
