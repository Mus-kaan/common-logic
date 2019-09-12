//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.AzureAsyncOperation.DataSource
{
    public class AsyncOperationStatusDataSource : IAsyncOperationStatusDataSource
    {
        private readonly IMongoCollection<AsyncOperationStatusEntity> _collection;

        public AsyncOperationStatusDataSource(IMongoCollection<AsyncOperationStatusEntity> collection)
        {
            _collection = collection;
        }

        public async Task<AsyncOperationStatusEntity> CreateAsync(
            OperationStatus status,
            TimeSpan timeout,
            string errorCode = null,
            string errorMessage = null)
        {
            var id = Guid.NewGuid().ToString();
            var entity = new AsyncOperationStatusEntity()
            {
                Id = id,
                Resource = new AsyncOperationResource()
                {
                    OperationId = id,
                    Status = OperationStatus.Created,
                    Error = new Error()
                    {
                        Code = errorCode,
                        Message = errorMessage,
                    },
                    StartTime = DateTime.UtcNow,
                },
                Timeout = timeout,
            };
            await _collection.InsertOneAsync(entity);

            return entity;
        }

        public async Task<AsyncOperationStatusEntity> UpdateAsync(
            string operationId,
            OperationStatus status,
            string errorCode = null,
            string errorMessage = null)
        {
            var updateDef = Builders<AsyncOperationStatusEntity>.Update
                .Set(o => o.Resource.Status, status)
                .Set(o => o.Resource.Error, new Error()
                {
                    Code = errorCode,
                    Message = errorMessage,
                });
            var filter = Builders<AsyncOperationStatusEntity>.Filter.Eq(u => u.Id, operationId);
            return await _collection.FindOneAndUpdateAsync(filter, updateDef);
        }

        public async Task<AsyncOperationStatusEntity> GetAsync(string operationId)
        {
            var filter = Builders<AsyncOperationStatusEntity>.Filter.Eq(u => u.Id, operationId);
            var cursor = await _collection.FindAsync(filter);
            var operation = await cursor.FirstOrDefaultAsync();

            // Return cancellation status for timed out operations.
            if (DateTime.UtcNow.Subtract(operation.Resource.StartTime) > operation.Timeout)
            {
                operation = await UpdateAsync(operationId, OperationStatus.Canceled);
            }

            return operation;
        }
    }
}
