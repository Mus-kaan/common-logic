//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IdempotentRPWorker.Interfaces;
using Microsoft.Liftr.Queue;
using Serilog;
using System.Threading;

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class RPWorkerDataBuilder
    {
        public LiftrQueueMessage QueueMessage { get; set; }

        public IWorkerDatabaseService DbSvc { get; set; }

        public ILogger Logger { get; set; }

        public QueueReaderOptions QueueReaderOptions { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public StateContext StateContext { get; set; }

        public MarketplaceBuilder MarketplaceBuilder { get; set; }
    }
}
