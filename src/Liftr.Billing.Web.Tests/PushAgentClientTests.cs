//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    public class PushAgentClientTests
    {
        private UsageEvent _sampleUsageEvent = new UsageEvent()
        {
            SubscriptionId = Guid.Parse("8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64"),
            EventId = Guid.NewGuid(),
            MeterId = "meterId",
            EventDateTime = DateTime.Now,
            Location = "westus",
            Quantity = 100,
            ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
        };

        [Fact]
        public async Task InsertSingleUsage_Inserts_Event_Into_Table_Async()
        {
            var testBillingServiceProvider = new TestBillingServiceProvider();

            await testBillingServiceProvider.Object.GetPushAgentClient().TryInsertSingleUsageAsync(_sampleUsageEvent, CancellationToken.None);

            Assert.Single(testBillingServiceProvider.UsageTable.UsageRecordsList);

            testBillingServiceProvider.UsageTable
                .Verify(e => e.ExecuteAsync(It.IsAny<TableOperation>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task InsertSingleUsage_Inserts_Event_Into_Queue_Async()
        {
            var testBillingServiceProvider = new TestBillingServiceProvider();

            await testBillingServiceProvider.Object.GetPushAgentClient().TryInsertSingleUsageAsync(_sampleUsageEvent, CancellationToken.None);

            testBillingServiceProvider.UsageQueue
                .Verify(e => e.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task InsertBatchUsage_Inserts_Event_Into_Queue_Async()
        {
            var testBillingServiceProvider = new TestBillingServiceProvider();

            var batchUsageEvent = new BatchUsageEvent
            {
                UsageEvents = new List<UsageEvent>()
                {
                    _sampleUsageEvent,
                },
            };

            await testBillingServiceProvider.Object.GetPushAgentClient().TryInsertBatchUsageAsync(batchUsageEvent, CancellationToken.None);

            testBillingServiceProvider.UsageQueue
                .Verify(e => e.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task InsertBatchUsage_Inserts_Batch_Into_Table_Async()
        {
            var testBillingServiceProvider = new TestBillingServiceProvider();

            var batchUsageEvent = new BatchUsageEvent
            {
                UsageEvents = new List<UsageEvent>()
                {
                    _sampleUsageEvent,
                },
            };

            await testBillingServiceProvider.Object.GetPushAgentClient().TryInsertBatchUsageAsync(batchUsageEvent, CancellationToken.None);

            testBillingServiceProvider.UsageTable
                .Verify(e => e.ExecuteBatchAsync(It.IsAny<TableBatchOperation>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
