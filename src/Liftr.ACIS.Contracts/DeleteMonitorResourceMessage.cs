//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class DeleteMonitorResourceMessage
    {
        /// <summary>
        /// The resource Id of the monitor resource.
        /// </summary>
        public string MonitorId { get; set; }

        /// <summary>
        /// The resource type of the monitor resource.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Type of marketplace resource- tenant level or subscription level.
        /// </summary>
        public bool IsTenantLevelMarketplaceResource { get; set; }

        /// <summary>
        /// Option for deleting marketplace resource.
        /// </summary>
        public bool IsDeleteMarketplaceResource { get; set; }

        /// <summary>
        /// Option for notifying partner.
        /// </summary>
        public bool IsNotifyPartner { get; set; }

        /// <summary>
        /// Option for continuing on failure.
        /// </summary>
        public bool IsForcefulDelete { get; set; }

        /// <summary>
        /// Option for deleting from RPaaS.
        /// </summary>
        public bool IsRPaaSDelete { get; set; }
    }
}