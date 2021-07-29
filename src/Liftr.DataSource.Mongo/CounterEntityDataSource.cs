//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class CounterEntityDataSource : ICounterEntityDataSource
    {
        private readonly IMongoCollection<CounterEntity> _collection;
        private readonly MongoWaitQueueRateLimiter _rateLimiter;
        private readonly ITimeSource _timeSource;
        private readonly string _collectionName;
        private readonly Serilog.ILogger _logger;

        public CounterEntityDataSource(IMongoCollection<CounterEntity> collection, MongoWaitQueueRateLimiter rateLimiter, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _collectionName = _collection?.CollectionNamespace?.CollectionName ?? throw new InvalidOperationException("Cannot find collection name");
            _logger = rateLimiter.Logger; // Although this looks hacky, changing required function signature will need lots of down stream change.
        }

        public async Task IncreaseCounterAsync(string counterName, int incrementValue = 1)
        {
            if (string.IsNullOrEmpty(counterName))
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            counterName = counterName.ToLowerInvariant();
            var timestamp = _timeSource.UtcNow;
            var filter = Builders<CounterEntity>.Filter.Eq(u => u.CounterKey, counterName);
            var update = Builders<CounterEntity>.Update.Inc(item => item.CounterValue, incrementValue)
                .Set(item => item.LastModifiedUTC, timestamp)
                .SetOnInsert(item => item.CounterKey, counterName)
                .SetOnInsert(item => item.CreatedUTC, timestamp);

            using var op = _logger.StartTimedOperation($"{_collectionName}-{nameof(IncreaseCounterAsync)}");
            await _rateLimiter.WaitAsync();
            try
            {
                var updateResult = await _collection.UpdateOneAsync(filter, update, new UpdateOptions() { IsUpsert = true });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(IncreaseCounterAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<int?> GetCounterAsync(string counterName)
        {
            if (string.IsNullOrEmpty(counterName))
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            counterName = counterName.ToLowerInvariant();
            var filter = Builders<CounterEntity>.Filter.Eq(u => u.CounterKey, counterName);

            using var op = _logger.StartTimedOperation($"{_collectionName}-{nameof(GetCounterAsync)}");
            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync(filter);
                var counterEntity = await cursor.FirstOrDefaultAsync();

                return counterEntity?.CounterValue;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(GetCounterAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        public async Task<IDictionary<string, int>> ListCountersAsync(string prefix = null)
        {
            var filter = Builders<CounterEntity>.Filter.Empty;
            if (!string.IsNullOrEmpty(prefix))
            {
                filter = Builders<CounterEntity>.Filter.Where(item => item.CounterKey.Contains(prefix));
            }

            using var op = _logger.StartTimedOperation($"{_collectionName}-{nameof(ListCountersAsync)}");
            await _rateLimiter.WaitAsync();
            try
            {
                var cursor = await _collection.FindAsync(filter);
                var counters = await cursor.ToListAsync();
                Dictionary<string, int> result = new Dictionary<string, int>();

                foreach (var counter in counters)
                {
                    result[counter.CounterKey] = counter.CounterValue;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(ListCountersAsync)} failed");
                op?.FailOperation(ex.Message);
                throw;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}
