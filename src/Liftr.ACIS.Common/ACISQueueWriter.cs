//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage.Queue;
using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.ClassicQueue;
using Microsoft.Liftr.Contracts;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Common
{
    public class ACISQueueWriter
    {
        private readonly ACISOperationStorageOptions _options;
        private readonly Serilog.ILogger _logger;
        private readonly ClassicQueueWriter _writer;

        public ACISQueueWriter(ACISOperationStorageOptions options, Serilog.ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Azure.Storage.CloudStorageAccount classicStorageAccount = Azure.Storage.CloudStorageAccount.Parse(options.StorageAccountConnectionString);
            CloudQueueClient queueClient = classicStorageAccount.CreateCloudQueueClient();
            var classicQueue = queueClient.GetQueueReference(options.OperationNotificationQueueName);
            _writer = new ClassicQueueWriter(classicQueue, new SystemTimeSource(), logger);
        }

        public Task NotifyACISOperationAsync(string operationName, string operationId, string parameters)
        {
            var queueMessage = new ACISOperationQueueMessage()
            {
                Operation = operationName,
                OperationId = operationId,
                Parameters = parameters,
            };

            return _writer.AddMessageAsync(JsonConvert.SerializeObject(queueMessage));
        }
    }
}
