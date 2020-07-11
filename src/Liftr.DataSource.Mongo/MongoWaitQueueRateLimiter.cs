//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    /// <summary>
    /// There exists a DB connection pool. It has a wait queue limit.
    /// If the limit is reached, the SDK will throw a <see cref="MongoWaitQueueFullException"/>.
    /// This class add a rate limiter to make sure the Max Wait queue limit is not reached.
    /// When there are too many concurrent DB requests, the later ones will need to wait for the previous ones to finish before they can start.
    /// </summary>
    public sealed class MongoWaitQueueRateLimiter : IDisposable
    {
        private readonly SemaphoreSlim _mu;
        private readonly Serilog.ILogger _logger;

        public MongoWaitQueueRateLimiter(int maxConcurrency, Serilog.ILogger logger)
        {
            _mu = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task WaitAsync()
        {
            if (_mu.CurrentCount == 2)
            {
                _logger.Information("There is not so much concurrency quota left. Extra concurrent consumers need to enter wait queue.");
            }

            return _mu.WaitAsync();
        }

        public void Release()
        {
            _mu.Release();
        }

        public void Dispose()
        {
            _mu.Dispose();
        }
    }
}
