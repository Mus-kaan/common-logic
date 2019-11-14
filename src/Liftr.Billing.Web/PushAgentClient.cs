//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Billing.Web
{
    public class PushAgentClient
    {
        private readonly CloudTable _pushAgentUsageTable;
        private readonly CloudQueue _pushAgentUsageQueue;
        private readonly Serilog.ILogger _logger;

        public PushAgentClient(IBillingServiceProvider provider, Serilog.ILogger logger)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _pushAgentUsageTable = provider.GetPushAgentUsageTable();
            _pushAgentUsageQueue = provider.GetPushAgentUsageQueue();
            _logger = logger;
        }

        public async Task<bool> TryInsertSingleUsageAsync(UsageEvent usageEvent, CancellationToken cancellationToken)
        {
            if (usageEvent is null)
            {
                throw new ArgumentNullException(nameof(usageEvent));
            }

            var partitionKey = Guid.NewGuid().ToString();
            var usageRecord = UsageRecordEntity.From(usageEvent, partitionKey);

            try
            {
                var insertedRecord = await TryInsertOrMergeRecordAsync(usageRecord, cancellationToken);
                await TryPostMessageToQueueAsync(insertedRecord.PartitionKey, cancellationToken);
                return true;
            }
            catch (StorageException e)
            {
                _logger.Error(
                    "Failed to insert usage record {@billingEvent} with {@error}",
                    new BillingEvent()
                    {
                        OperationName = nameof(this.TryInsertSingleUsageAsync),
                        EventId = usageRecord.EventId,
                    }, e.Message);

                return false;
            }
        }

        public async Task<bool> TryInsertBatchUsageAsync(BatchUsageEvent batchUsageEvent, CancellationToken cancellationToken)
        {
            if (batchUsageEvent is null)
            {
                throw new ArgumentNullException(nameof(batchUsageEvent));
            }

            try
            {
                var partitionKey = await TryInsertBatchAsync(batchUsageEvent, cancellationToken);
                await TryPostMessageToQueueAsync(partitionKey, cancellationToken);
                return true;
            }
            catch (StorageException e)
            {
                _logger.Error(
                    "Failed to insert usage record {@billingEvent} with {@error}",
                    new BillingEvent()
                    {
                        OperationName = nameof(this.TryInsertSingleUsageAsync),
                    }, e.Message);

                return false;
            }
        }

        private async Task TryPostMessageToQueueAsync(string partitionKey, CancellationToken cancellationToken)
        {
            var pushAgentBatchId = Guid.NewGuid();
            var message = new PushAgentUsageQueueMessage(partitionKey, pushAgentBatchId).ToJson();

            await _pushAgentUsageQueue.AddMessageAsync(new CloudQueueMessage(message), cancellationToken);

            _logger.Information(
                "[UsageQueueInsert]: Inserted a usage record {@billingEvent}",
                new BillingEvent()
                {
                    OperationName = nameof(this.TryPostMessageToQueueAsync),
                    PartitionKey = partitionKey,
                    PushAgentBatchId = pushAgentBatchId,
                });
        }

        private async Task<UsageRecordEntity> TryInsertOrMergeRecordAsync(UsageRecordEntity usageRecordEntity, CancellationToken cancellationToken)
        {
            var operation = TableOperation.InsertOrMerge(usageRecordEntity);
            var tableResult = await _pushAgentUsageTable.ExecuteAsync(operation, cancellationToken);

            var insertedRecord = tableResult.Result as UsageRecordEntity;
            var pushAgentBatchId = Guid.NewGuid();

            _logger.Information(
                "[UsageTableInsert]: Inserted a usage record {@billingEvent}",
                new BillingEvent()
                {
                    OperationName = nameof(this.TryInsertSingleUsageAsync),
                    PartitionKey = insertedRecord.PartitionKey,
                    RowKey = insertedRecord.RowKey,
                    EventId = insertedRecord.EventId,
                });

            return insertedRecord;
        }

        private async Task<string> TryInsertBatchAsync(BatchUsageEvent batchUsageEvent, CancellationToken cancellationToken)
        {
            var operation = new TableBatchOperation();

            var partitionKey = Guid.NewGuid().ToString();
            foreach (var usageEvent in batchUsageEvent.UsageEvents)
            {
                operation.InsertOrMerge(UsageRecordEntity.From(usageEvent, partitionKey));
            }

            var tablebatchResult = await _pushAgentUsageTable.ExecuteBatchAsync(operation, cancellationToken);
            var pushAgentBatchId = Guid.NewGuid();

            _logger.Information(
                "[UsageTableInsert]: Inserted a batch usage record {@billingEvent}",
                new BillingEvent()
                {
                    OperationName = nameof(this.TryInsertBatchAsync),
                    PartitionKey = partitionKey,
                });

            return partitionKey;
        }
    }
}
