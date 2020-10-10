//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Liftr.ACIS.Logging;
using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Logging.StaticLogger;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Common
{
    /// <summary>
    /// This is used by the Geneva Actions to send the work requests to ACIS worker (in RP).
    /// Relay process
    /// 1. For each operation, geneva action will assign an operation name and operation Id.
    /// 2. Geneva action create a row in Azure table for the operation.
    /// 3. Geneva action put a message about the operation into Azure queue.
    /// 4. Geneva action keep polling the Azure table row to know the operation status, i.e. extract logs and wait for finish.
    /// 5. Similar to RP worker, ACIS worker will pick up the queue message and do the actual work.
    /// ACIS worker can write intermediate updates to the Azure table row and send back to Geneva action.
    /// After finish, the ACIS worker will write the result in the Azure Table.
    /// 6. Geneva action poll the operation status from table until it is finished. It will serve the result to clients.
    /// </summary>
    public class ACISWorkCoordinator : IACISWorkCoordinator
    {
        private static readonly TimeSpan s_pollingInterval = TimeSpan.FromSeconds(1);

        private readonly ACISOperationStorageOptions _options;
        private readonly ITimeSource _timeSource;
        private readonly IAcisLogger _logger;
        private readonly TimeSpan _timeout;
        private readonly ACISOperationStatusEntityDataSource _dataSource;
        private readonly ACISQueueWriter _q;

        public ACISWorkCoordinator(ACISOperationStorageOptions options, ITimeSource timeSource, IAcisLogger logger, TimeSpan? timeout = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeout = timeout.HasValue ? timeout.Value : TimeSpan.FromMinutes(5);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(options.StorageAccountConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference(_options.OperationStatusTableName);
            _dataSource = new ACISOperationStatusEntityDataSource(table);

            _q = new ACISQueueWriter(options, logger.Logger);
        }

        public async Task<ACISWorkResult> StartWorkAsync(string operationName, string parameters, string operationId = null)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                operationId = $"{operationName}-{_timeSource.UtcNow.ToString("MMddHmmss", CultureInfo.InvariantCulture)}-{Guid.NewGuid()}";
            }

            try
            {
                using (var ops = _logger.StartTimedOperation(operationName, operationId))
                {
                    try
                    {
                        var status = await _dataSource.InsertEntityAsync(operationName, operationId);
                        _logger.LogInfo($"Added ACIS operation status to RP table. operationName: '{operationName}', operationId: '{operationId}'");

                        await _q.NotifyACISOperationAsync(operationName, operationId, parameters);
                        _logger.LogInfo($"Added to RP notification queue. operationName: '{operationName}', operationId: '{operationId}'");

                        var endTime = _timeSource.UtcNow.Add(_timeout);

                        while (_timeSource.UtcNow < endTime)
                        {
                            await Task.Delay(s_pollingInterval);
                            var newStatus = await _dataSource.GetEntityAsync(operationName, operationId);

                            if (newStatus == null)
                            {
                                throw new InvalidOperationException($"Cannot find the ACIS operation entity with operationId '{operationId}'");
                            }

                            CheckLogs(status, newStatus);
                            status = newStatus;
                            if (status.IsFinished())
                            {
                                if (status.Status == ACISOperationStatusType.Succeeded)
                                {
                                    await _dataSource.DeleteEntityAsync(operationName, operationId);
                                }

                                return new ACISWorkResult()
                                {
                                    Succeeded = status.Status == ACISOperationStatusType.Succeeded,
                                    Result = status.Result,
                                };
                            }

                            _logger.LogVerbose($"[{operationId}] Polling operation status ...");
                        }

                        var timeoutMessage = "RP is not responding within the timeout. Abort.";
                        ops.FailOperation(timeoutMessage);
                        _logger.LogError(timeoutMessage);
                        return new ACISWorkResult()
                        {
                            Succeeded = false,
                            Result = timeoutMessage,
                        };
                    }
                    catch (Exception ex)
                    {
                        ops.FailOperation(ex.Message);
                        _logger.LogError(ex, $"{operationName} failed");
                        return new ACISWorkResult()
                        {
                            Succeeded = false,
                            Result = ex.Message,
                        };
                    }
                }
            }
            finally
            {
                StaticLiftrLogger.Flush();
            }
        }

        private void CheckLogs(ACISOperationStatusEntity oldStatus, ACISOperationStatusEntity newStatus)
        {
            var oldLogs = oldStatus.GetLogEntries();
            var newLogs = newStatus.GetLogEntries();

            if (oldLogs.Count() >= newLogs.Count())
            {
                return;
            }

            var newAddedOnes = newLogs.Skip(oldLogs.Count());

            foreach (var log in newAddedOnes)
            {
                var msgAttribute = string.IsNullOrEmpty(log.Machine) ? log.TimeStamp : $"{log.Machine}|{log.TimeStamp}";
                var msg = $"[{msgAttribute}] {log.Message}";
                switch (log.Level)
                {
                    case LogLevel.Error:
                        {
                            _logger.LogError(msg);
                            break;
                        }

                    case LogLevel.Warning:
                        {
                            _logger.LogWarning(msg);
                            break;
                        }

                    case LogLevel.Info:
                        {
                            _logger.LogInfo(msg);
                            break;
                        }

                    case LogLevel.Verbose:
                        {
                            _logger.LogVerbose(msg);
                            break;
                        }
                }
            }
        }
    }
}
