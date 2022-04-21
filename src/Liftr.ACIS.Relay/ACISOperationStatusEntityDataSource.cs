//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Relay
{
    public class ACISOperationStatusEntityDataSource : IACISOperationStatusEntityDataSource
    {
        private readonly TableClient _tableClient;

        public ACISOperationStatusEntityDataSource(TableClient tableClient)
        {
            _tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        }

        public async Task<ACISOperationStatusEntity> InsertEntityAsync(string operationName, string operationId)
        {
            var entity = new ACISOperationStatusEntity(operationName, operationId);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Merge);
            return entity;
        }

        public async Task<ACISOperationStatusEntity> GetEntityAsync(string operationName, string operationId)
        {
            var result = new ACISOperationStatusEntity();
            try
            {
                result = await _tableClient.GetEntityAsync<ACISOperationStatusEntity>(operationName, operationId);
            }
            catch (RequestFailedException exception)
            {
                if (exception.Status == 404)
                {
                    return null;
                }
            }

            return result;
        }

        public async Task<ACISOperationStatusEntity> UpdateEntityAsync(ACISOperationStatusEntity entity)
        {
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            return entity;
        }

        public async Task DeleteEntityAsync(string operationName, string operationId)
        {
            var entity = await GetEntityAsync(operationName, operationId);

            if (entity == null)
            {
                return;
            }

            await _tableClient.DeleteEntityAsync(operationName, operationId);
        }
    }
}
