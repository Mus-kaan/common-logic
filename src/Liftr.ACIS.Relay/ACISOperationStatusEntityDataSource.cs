//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Relay
{
    public class ACISOperationStatusEntityDataSource
    {
        private readonly CloudTable _table;

        public ACISOperationStatusEntityDataSource(CloudTable table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
        }

        public async Task<ACISOperationStatusEntity> InsertEntityAsync(string operationName, string operationId)
        {
            var entity = new ACISOperationStatusEntity(operationName, operationId);

            TableOperation insertOperation = TableOperation.Insert(entity);
            var insertResult = await _table.ExecuteAsync(insertOperation);

            return insertResult.Result as ACISOperationStatusEntity;
        }

        public async Task<ACISOperationStatusEntity> GetEntityAsync(string operationName, string operationId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<ACISOperationStatusEntity>(operationName, operationId);
            TableResult result = await _table.ExecuteAsync(retrieveOperation);

            if (result.HttpStatusCode == 404)
            {
                return null;
            }

            return result.Result as ACISOperationStatusEntity;
        }

        public async Task<ACISOperationStatusEntity> UpdateEntityAsync(ACISOperationStatusEntity entity)
        {
            var operation = TableOperation.Replace(entity);
            var result = await _table.ExecuteAsync(operation);
            return result.Result as ACISOperationStatusEntity;
        }

        public async Task DeleteEntityAsync(string operationName, string operationId)
        {
            var entity = await GetEntityAsync(operationName, operationId);

            if (entity == null)
            {
                return;
            }

            TableOperation deleteOperation = TableOperation.Delete(entity);
            await _table.ExecuteAsync(deleteOperation);
        }
    }
}
