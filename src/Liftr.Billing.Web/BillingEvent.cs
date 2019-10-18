//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Billing.Web
{
    internal class BillingEvent
    {
        public string OperationName { get; set; }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public Guid EventId { get; set; }

        public Guid PushAgentBatchId { get; set; }
    }
}