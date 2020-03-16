﻿//-----------------------------------------------------------------------------
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
        private readonly ITimeSource _timeSource;

        public CounterEntityDataSource(IMongoCollection<CounterEntity> collection, ITimeSource timeSource)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
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

            var updateResult = await _collection.UpdateOneAsync(filter, update, new UpdateOptions() { IsUpsert = true });
        }

        public async Task<int?> GetCounterAsync(string counterName)
        {
            if (string.IsNullOrEmpty(counterName))
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            counterName = counterName.ToLowerInvariant();
            var filter = Builders<CounterEntity>.Filter.Eq(u => u.CounterKey, counterName);

            var cursor = await _collection.FindAsync(filter);
            var counterEntity = await cursor.FirstOrDefaultAsync();

            return counterEntity?.CounterValue;
        }

        public async Task<IDictionary<string, int>> ListCountersAsync(string prefix = null)
        {
            var filter = Builders<CounterEntity>.Filter.Empty;
            if (!string.IsNullOrEmpty(prefix))
            {
                filter = Builders<CounterEntity>.Filter.Where(item => item.CounterKey.Contains(prefix));
            }

            var cursor = await _collection.FindAsync(filter);
            var counters = await cursor.ToListAsync();
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (var counter in counters)
            {
                result[counter.CounterKey] = counter.CounterValue;
            }

            return result;
        }
    }
}