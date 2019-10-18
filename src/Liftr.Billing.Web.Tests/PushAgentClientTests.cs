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
        [Fact]
        public async Task InsertSingleUsage_Inserts_Event_Into_Table_Async()
        {
            var testBillingServiceProvider = new TestBillingServiceProvider();

            var list = new List<UsageRecordEntity>();
            var record = new UsageRecordEntity()
            {
                PartitionKey = "test",
                RowKey = "test",
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            await testBillingServiceProvider.Object.GetPushAgentClient().TryInsertSingleUsageAsync(record, CancellationToken.None);

            Assert.Single(testBillingServiceProvider.UsageTable.UsageRecordsList);

            testBillingServiceProvider.UsageTable
                .Verify(e => e.ExecuteAsync(It.IsAny<TableOperation>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task InsertSingleUsage_Inserts_Event_Into_Queue_Async()
        {
            var testBillingServiceProvider = new TestBillingServiceProvider();

            var list = new List<UsageRecordEntity>();
            var record = new UsageRecordEntity()
            {
                PartitionKey = "test",
                RowKey = "test",
                ResourceUri = "/subscriptions/8b7a0bae-e1a1-4c5a-8829-3c32c08dcc64/resourceGroups/Flying-SuperMan-Group/providers/Microsoft.ClassicCompute/VirtualMachines/vm0​",
            };

            await testBillingServiceProvider.Object.GetPushAgentClient().TryInsertSingleUsageAsync(record, CancellationToken.None);

            testBillingServiceProvider.UsageQueue
                .Verify(e => e.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
