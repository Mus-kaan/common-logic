//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.Saas.Contracts;

namespace Microsoft.Liftr.Marketplace.WebhookQueue.Contracts
{
    public class WebhookWorkerQueueMessage
    {
        public WebhookPayload WebhookPayload { get; set; }
    }
}
