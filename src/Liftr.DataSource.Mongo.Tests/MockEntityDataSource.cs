//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public class MockEntityDataSource : ResourceEntityDataSource<MockResourceEntity>
    {
        public MockEntityDataSource(IMongoCollection<MockResourceEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
            : base(collection, rateLimiter, timeSource, enableOptimisticConcurrencyControl: true, logOperation: true)
        {
        }
    }

    public class MockExtendedEntityDataSource : ResourceEntityDataSource<ExtendedMockResourceEntity>
    {
        public MockExtendedEntityDataSource(IMongoCollection<ExtendedMockResourceEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
            : base(collection, rateLimiter, timeSource, enableOptimisticConcurrencyControl: true, logOperation: true)
        {
        }
    }
}
