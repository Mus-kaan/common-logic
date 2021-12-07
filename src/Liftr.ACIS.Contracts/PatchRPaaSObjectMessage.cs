//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class PatchRPaaSObjectMessage
    {
        /// <summary>
        /// The resource Id of the monitor resource.
        /// </summary>
        public string MonitorId { get; set; }

        /// <summary>
        /// The RPaaS object.
        /// </summary>
        public string RPaaSobject { get; set; }
    }
}
