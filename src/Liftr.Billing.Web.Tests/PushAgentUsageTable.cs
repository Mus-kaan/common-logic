//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Liftr.Billing;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    internal class PushAgentUsageTable : CloudTableMockBase
    {
        public List<UsageRecordEntity> UsageRecordsList = new List<UsageRecordEntity>();

        private static Uri s_tableUri = new Uri("https://fakestorage.table.core.windows.net/usageTable");

        public PushAgentUsageTable()
            : base(MockBehavior.Strict, s_tableUri)
        {
            Setup(t => t.ExecuteBatchAsync(It.IsAny<TableBatchOperation>(), It.IsAny<CancellationToken>()))
                .Returns((TableBatchOperation batchOperation, CancellationToken token) =>
                {
                    var batchResult = new TableBatchResult();

                    foreach (var operation in batchOperation)
                    {
                        batchResult.Add(new TableResult() { Result = operation.Entity });
                        UsageRecordsList.Add((UsageRecordEntity)operation.Entity);
                    }

                    return Task.FromResult(batchResult);
                });

            Setup(t => t.ExecuteAsync(It.IsAny<TableOperation>(), It.IsAny<CancellationToken>()))
                .Returns((TableOperation operation, CancellationToken token) =>
                {
                    if (operation.OperationType == TableOperationType.InsertOrMerge)
                    {
                        UsageRecordsList.Add((UsageRecordEntity)operation.Entity);
                    }

                    return Task.FromResult(new TableResult() { Result = operation.Entity });
                });
        }
    }
}