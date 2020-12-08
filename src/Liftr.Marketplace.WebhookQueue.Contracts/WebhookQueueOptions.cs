//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace.WebhookQueue.Contracts
{
    public class WebhookQueueOptions
    {
        /// <summary>
        /// Name of the Webhook worker queue
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// The number of maximum concurrent calls allowed by the Webhook Worker queue reader.
        /// </summary>
        public int QueueListenerCount { get; set; }

        /// <summary>
        /// The number of times the message will be dequeued from the queue in case of failures.
        /// </summary>
        public int MaxDequeueCount { get; set; }
    }
}
