//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.VNext.Common.Interfaces;
using Microsoft.Liftr.Queue;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.Common
{
    public class WhaleDualVersionQueueWriter : IWhaleDualVersionQueueWriter
    {
        private readonly ILogger _logger;
        private IQueueWriter _whaleQueueWriter;
        private IQueueWriter _whaleV2QueueWriter;
        private WhaleSubscriptionOptions _whaleSubscriptionOptions;

        public WhaleDualVersionQueueWriter(
            IQueueWriter whaleQueueWriter,
            IQueueWriter whaleV2QueueWriter,
            WhaleSubscriptionOptions whaleSubscriptionOptions,
            ILogger logger)
        {
            _whaleQueueWriter = whaleQueueWriter ?? throw new ArgumentNullException(nameof(whaleQueueWriter));
            _whaleV2QueueWriter = whaleV2QueueWriter ?? throw new ArgumentNullException(nameof(whaleV2QueueWriter));
            _whaleSubscriptionOptions = whaleSubscriptionOptions ?? throw new ArgumentNullException(nameof(whaleSubscriptionOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddMessageAsync(string subscriptionId, string message, CancellationToken cancellationToken = default)
        {
            if (_whaleSubscriptionOptions.IsV2Subscription(subscriptionId))
            {
                Log.Verbose("Writing to Whale V2 Queue");
                await _whaleV2QueueWriter.AddMessageAsync(message, cancellationToken: cancellationToken);
            }
            else
            {
                Log.Verbose("Writing to Whale V1 Queue");
                await _whaleQueueWriter.AddMessageAsync(message, cancellationToken: cancellationToken);
            }
        }
    }
}