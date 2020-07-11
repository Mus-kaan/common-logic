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
            : base(collection, rateLimiter, timeSource)
        {
        }
    }
}
