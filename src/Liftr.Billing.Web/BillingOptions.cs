//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Billing.Web
{
    public class BillingOptions
    {
        public string PushAgentStorageConnectionString { get; set; }

        public string UsageRecordsToPushAgentTableName { get; set; }

        public string UsageRecordsToPushAgentQueueName { get; set; }

        public string StatusRecordsQueueName { get; set; }
    }
}