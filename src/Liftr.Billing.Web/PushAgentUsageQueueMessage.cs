//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Billing.Web
{
    public class PushAgentUsageQueueMessage
    {
        public PushAgentUsageQueueMessage(string partitionKey, Guid batchId)
        {
            PartitionKey = partitionKey;
            BatchId = batchId;
        }

        public string PartitionKey { get; set; }

        public Guid BatchId { get; set; }
    }
}
