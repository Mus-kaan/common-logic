//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Queue;
using Microsoft.Liftr.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Worker
{
    public class ACISWorker : BackgroundService
    {
        private readonly IACISOperationProcessor _operationProcessor;
        private readonly ITimeSource _timeSource;
        private readonly IQueueReader _workerQueueReader;
        private readonly IACISOperationStatusEntityDataSource _acisStatusDataSource;
        private readonly Serilog.ILogger _logger;
        private readonly string _machineName;

        public ACISWorker(
            IACISOperationProcessor operationProcessor,
            ITimeSource timeSource,
            IQueueReader workerQueueReader,
            IACISOperationStatusEntityDataSource acisStatusDataSource,
            Serilog.ILogger logger)
        {
            _operationProcessor = operationProcessor ?? throw new ArgumentNullException(nameof(operationProcessor));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _workerQueueReader = workerQueueReader ?? throw new ArgumentNullException(nameof(workerQueueReader));
            _acisStatusDataSource = acisStatusDataSource ?? throw new ArgumentNullException(nameof(acisStatusDataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _machineName = Environment.MachineName;
            var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
            if (!string.IsNullOrEmpty(meta?.InstanceMeta?.Compute?.Name))
            {
                _machineName = meta?.InstanceMeta?.Compute?.Name;
            }
        }

        public async Task ProcessACISQueueMessageAsync(
           LiftrQueueMessage message,
           QueueMessageProcessingResult result,
           CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            using (var operation = _logger.StartTimedOperation(nameof(ProcessACISQueueMessageAsync)))
            {
                var dequeueCount = message.DequeueCount;
                var workerMessage = message.Content.FromJson<ACISOperationQueueMessage>();
                var acisOperationName = workerMessage.Operation;
                var acisOperationId = workerMessage.OperationId;

                operation.SetContextProperty(nameof(acisOperationName), acisOperationName);
                operation.SetContextProperty(nameof(acisOperationId), acisOperationId);
                operation.SetContextProperty(nameof(dequeueCount), dequeueCount);

                try
                {
                    var statusEntity = await _acisStatusDataSource.GetEntityAsync(acisOperationName, acisOperationId);
                    if (statusEntity == null)
                    {
                        var msg = "err_no_acis_status. Cannot find the ACIS status in table. Abort.";
                        operation.FailOperation(msg);
                        _logger.Error(msg);
                        return;
                    }

                    var acisOperation = new ACISOperation(statusEntity, _acisStatusDataSource, _timeSource, _machineName);

                    var req = new ACISOperationRequest()
                    {
                        Operation = acisOperation,
                        OperationName = acisOperationName,
                        OperationId = acisOperationId,
                        Parameters = workerMessage.Parameters,
                    };

                    await _operationProcessor.ProcessAsync(req);

                    result.SuccessfullyProcessed = true;
                    result.ProcessingError = null;
                    operation.SetResultDescription("Successfully processed ACIS queue message.");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "An error occurred while working on ACIS operation.");

                    result.SuccessfullyProcessed = false;
                    result.ProcessingError = ex.Message;
                    operation.FailOperation(ex.Message);

                    throw;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Starting ACIS worker ...");

            await _workerQueueReader.StartListeningAsync(ProcessACISQueueMessageAsync, stoppingToken);
        }
    }
}