//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Relay
{
    /// <summary>
    /// This class is used by ACIS workers to manage the operation status.
    /// By using this, ACIS workers will write the status update in Azure table,
    /// the Geneva Action will poll the status from the table row.
    /// </summary>
    public class ACISOperation : IACISOperation
    {
        private readonly ACISOperationStatusEntityDataSource _acisStatusDataSource;
        private readonly ITimeSource _timeSource;
        private readonly ACISOperationStatusEntity _statusEntity;
        private readonly string _machineName;

        public ACISOperation(ACISOperationStatusEntity statusEntity, ACISOperationStatusEntityDataSource acisStatusDataSource, ITimeSource timeSource, string machineName = null)
        {
            _statusEntity = statusEntity ?? throw new ArgumentNullException(nameof(statusEntity));
            _acisStatusDataSource = acisStatusDataSource ?? throw new ArgumentNullException(nameof(acisStatusDataSource));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _machineName = machineName ?? Environment.MachineName;
        }

        public Task LogErrorAsync(string message)
        {
            var logEntry = new LogEntry()
            {
                Level = LogLevel.Error,
                TimeStamp = _timeSource.UtcNow.ToZuluString(),
                Message = message,
                Machine = _machineName,
            };

            return AddLogEntryAsync(logEntry);
        }

        public Task LogWarningAsync(string message)
        {
            var logEntry = new LogEntry()
            {
                Level = LogLevel.Warning,
                TimeStamp = _timeSource.UtcNow.ToZuluString(),
                Message = message,
                Machine = _machineName,
            };

            return AddLogEntryAsync(logEntry);
        }

        public Task LogInfoAsync(string message)
        {
            var logEntry = new LogEntry()
            {
                Level = LogLevel.Info,
                TimeStamp = _timeSource.UtcNow.ToZuluString(),
                Message = message,
                Machine = _machineName,
            };

            return AddLogEntryAsync(logEntry);
        }

        public Task LogVerboseAsync(string message)
        {
            var logEntry = new LogEntry()
            {
                Level = LogLevel.Verbose,
                TimeStamp = _timeSource.UtcNow.ToZuluString(),
                Message = message,
                Machine = _machineName,
            };

            return AddLogEntryAsync(logEntry);
        }

        public async Task SuccessfulFinishAsync(string result)
        {
            var entity = await _acisStatusDataSource.GetEntityAsync(_statusEntity.OperationName, _statusEntity.OperationId);

            entity.Status = ACISOperationStatusType.Succeeded;
            entity.Result = result;

            await _acisStatusDataSource.UpdateEntityAsync(entity);
        }

        public async Task FailAsync(string result)
        {
            var entity = await _acisStatusDataSource.GetEntityAsync(_statusEntity.OperationName, _statusEntity.OperationId);

            entity.Status = ACISOperationStatusType.Failed;
            entity.Result = result;

            await _acisStatusDataSource.UpdateEntityAsync(entity);
        }

        private async Task AddLogEntryAsync(LogEntry log)
        {
            var entity = await _acisStatusDataSource.GetEntityAsync(_statusEntity.OperationName, _statusEntity.OperationId);

            if (entity.Status == ACISOperationStatusType.Created)
            {
                entity.Status = ACISOperationStatusType.Started;
            }

            entity.AddLogEntry(log);

            await _acisStatusDataSource.UpdateEntityAsync(entity);
        }
    }
}
