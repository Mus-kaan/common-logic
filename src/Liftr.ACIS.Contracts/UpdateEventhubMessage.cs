//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class UpdateEventhubMessage
    {
        /// <summary>
        /// Name of the Eventhub namespace
        /// </summary>
        public string EventhubNamespaceName { get; set; }

        /// <summary>
        /// Should this eventhub be used to set in the Diagnostics settings for ingest OBO data.
        /// </summary>
        public bool IngestEnabled { get; set; }

        /// <summary>
        /// Should LogForwarder listen and read data from this eventhub.
        /// </summary>
        public bool Active { get; set; }
    }
}
