//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage.Queue;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    internal class PushAgentUsageQueue : Mock<CloudQueue>
    {
        public List<string> Messages = new List<string>();

        private static Uri s_queueUri = new Uri("https://fakestorage.queue.core.windows.net/usageQueue");

        public PushAgentUsageQueue()
            : base(MockBehavior.Strict, s_queueUri)
        {
            Setup(q => q.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<CancellationToken>()))
                .Returns((CloudQueueMessage msg, CancellationToken _) =>
                {
                    Messages.Add(msg.AsString);
                    return Task.FromResult(true);
                });
        }
    }
}