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
    public sealed class CounterEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<CounterEntity> _collectionScope;

        public CounterEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<CounterEntity>((db, collectionName) =>
            {
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.GetOrCreateCounterEntityCollectionAsync(collectionName).Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild]
        public async Task BasicCounterUsageTestAsync()
        {
            var ts = new MockTimeSource();
            ICounterEntityDataSource s = new CounterEntityDataSource(_collectionScope.Collection, ts);

            var prefix = "counter-prefix";

            for (int i = 0; i < 6; i++)
            {
                var prefixCount = (i % 2) + 1;
                var counterName = $"{prefix}{prefixCount}{prefixCount}{prefixCount}-{i}";
                {
                    await s.IncreaseCounterAsync(counterName, 0);
                    await Task.Delay(1000);
                    var val = await s.GetCounterAsync(counterName);
                    Assert.Equal(0, val);
                }

                {
                    await s.IncreaseCounterAsync(counterName);
                    var val = await s.GetCounterAsync(counterName);
                    Assert.Equal(1, val);
                }

                {
                    await s.IncreaseCounterAsync(counterName);
                    var val = await s.GetCounterAsync(counterName);
                    Assert.Equal(2, val);
                }

                {
                    await s.IncreaseCounterAsync(counterName, 10);
                    var val = await s.GetCounterAsync(counterName);
                    Assert.Equal(12, val);
                }

                {
                    await s.IncreaseCounterAsync(counterName, -20);
                    var val = await s.GetCounterAsync(counterName);
                    Assert.Equal(-8, val);
                }
            }

            {
                var counters = await s.ListCountersAsync();
                Assert.Equal(6, counters.Count);
            }

            {
                var counters = await s.ListCountersAsync("counter-prefix1");
                Assert.Equal(3, counters.Count);
            }

            {
                var counters = await s.ListCountersAsync("counter-prefix2");
                Assert.Equal(3, counters.Count);
            }
        }
    }
}
