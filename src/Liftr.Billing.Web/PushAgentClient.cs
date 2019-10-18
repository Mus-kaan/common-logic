//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using System;
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

        public async Task<bool> TryInsertSingleUsageAsync(UsageRecordEntity usageRecord, CancellationToken cancellationToken)
        {
            if (usageRecord is null)
            {
                throw new ArgumentNullException(nameof(usageRecord));
            }

            try
            {
                var insertedRecord = await TryInsertOrMergeRecordAsync(usageRecord, cancellationToken);
                await TryPostMessageToQueueAsync(insertedRecord, cancellationToken);
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

        private async Task TryPostMessageToQueueAsync(UsageRecordEntity usageRecord, CancellationToken cancellationToken)
        {
            var pushAgentBatchId = Guid.NewGuid();
            var message = new PushAgentUsageQueueMessage(usageRecord.PartitionKey, pushAgentBatchId).ToJson();

            await _pushAgentUsageQueue.AddMessageAsync(new CloudQueueMessage(message), cancellationToken);

            _logger.Information(
                "[UsageQueueInsert]: Inserted a usage record {@billingEvent}",
                new BillingEvent()
                {
                    OperationName = nameof(this.TryInsertSingleUsageAsync),
                    PartitionKey = usageRecord.PartitionKey,
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
    }
}
