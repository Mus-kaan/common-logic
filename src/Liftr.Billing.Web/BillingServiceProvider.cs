//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;

namespace Microsoft.Liftr.Billing.Web
{
    public class BillingServiceProvider : IBillingServiceProvider
    {
        private readonly Serilog.ILogger _logger;

        private CloudTableClient _pushAgentTableClient;
        private CloudQueueClient _pushAgentQueueClient;
        private CloudQueue _pushAgentErrorMessageQueue;
        private BillingOptions _billingOptions;

        public BillingServiceProvider(BillingOptions billingOptions, Serilog.ILogger logger, bool initialize = true)
        {
            _billingOptions = billingOptions;
            _logger = logger;

            if (initialize)
            {
                Initialize();
            }
        }

        public virtual PushAgentClient GetPushAgentClient() => new PushAgentClient(this, _logger);

        public virtual CloudTable GetPushAgentUsageTable()
        {
            var cloudTable = _pushAgentTableClient.GetTableReference(_billingOptions.UsageRecordsToPushAgentTableName);
            cloudTable.CreateIfNotExists();

            return cloudTable;
        }

        public virtual CloudQueue GetPushAgentUsageQueue()
        {
            var cloudQueue = _pushAgentQueueClient.GetQueueReference(_billingOptions.UsageRecordsToPushAgentQueueName);
            cloudQueue.CreateIfNotExists();
            return cloudQueue;
        }

        private void Initialize()
        {
            _pushAgentTableClient = CloudStorageAccount.Parse(_billingOptions.PushAgentStorageConnectionString).CreateCloudTableClient();
            _pushAgentQueueClient = Azure.Storage.CloudStorageAccount.Parse(_billingOptions.PushAgentStorageConnectionString).CreateCloudQueueClient();

            _pushAgentErrorMessageQueue = _pushAgentQueueClient.GetQueueReference(_billingOptions.StatusRecordsQueueName);
            _pushAgentErrorMessageQueue.CreateIfNotExists();
        }
    }
}