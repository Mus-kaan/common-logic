//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class RemoveRelationshipEntityMessage
    {
        /// <summary>
        /// Name of the Eventhub namespace
        /// </summary>
        public string ResoureId { get; set; }

        /// <summary>
        /// Name of the Eventhub namespace
        /// </summary>
        public string MonitorId { get; set; }
    }
}
