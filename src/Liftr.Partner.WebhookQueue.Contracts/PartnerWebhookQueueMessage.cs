//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Partner.WebhookQueue.Contracts
{
    public class PartnerWebhookQueueMessage
    {
        public PartnerActions Action { get; set; }

        public string WebhookPayload { get; set; }
    }
}
