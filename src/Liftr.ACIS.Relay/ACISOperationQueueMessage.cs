//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Relay
{
    public class ACISOperationQueueMessage
    {
        public string Operation { get; set; }

        public string OperationId { get; set; }

        public string Parameters { get; set; }
    }
}
