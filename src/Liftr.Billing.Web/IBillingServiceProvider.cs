//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;

namespace Microsoft.Liftr.Billing.Web
{
    public interface IBillingServiceProvider
    {
        PushAgentClient GetPushAgentClient();

        CloudQueue GetPushAgentUsageQueue();

        CloudTable GetPushAgentUsageTable();
    }
}