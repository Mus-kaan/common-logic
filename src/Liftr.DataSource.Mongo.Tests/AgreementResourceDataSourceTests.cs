//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public sealed class AgreementResourceDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<AgreementResourceEntity> _collectionScope;
        private readonly MockTimeSource _ts = new MockTimeSource();

        public AgreementResourceDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<AgreementResourceEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateAgreementEntityCollectionAsync(collectionName).Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
#pragma warning restore CS0618 // Type or member is obsolete
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task BasicDataSourceUsageAsync()
        {
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new AgreementResourceDataSource(_collectionScope.Collection, rateLimiter, _ts);

            var subscriptionId1 = Guid.NewGuid().ToString();

            var existing = await dataSource.GetAsync(subscriptionId1);
            Assert.False(existing);

            await dataSource.AcceptAsync(subscriptionId1);
            existing = await dataSource.GetAsync(subscriptionId1);
            Assert.True(existing);

            _ts.Add(TimeSpan.FromSeconds(10));
            await dataSource.AcceptAsync(subscriptionId1);
            existing = await dataSource.GetAsync(subscriptionId1);
            Assert.True(existing);
        }
    }
}
