//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ACIS.Contracts;
using Microsoft.Liftr.ACIS.Relay;
using Microsoft.Liftr.Contracts.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Management
{
    public class LogForwarderStorageManagement
    {
        private readonly IStorageEntityDataSource _lfStorageDataSource;

        public LogForwarderStorageManagement(IStorageEntityDataSource lfStorageDataSource)
        {
            _lfStorageDataSource = lfStorageDataSource ?? throw new ArgumentNullException(nameof(lfStorageDataSource));
        }

        public async Task<StorageListResult> ListStorageAsync(IACISOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            await operation.LogInfoAsync("Retrieving LogForwarder storage account meta data from DB ...");
            StorageListResult result = new StorageListResult();

            result.PrimaryStorageList = await _lfStorageDataSource.ListAsync(StoragePriority.Primary);
            result.BackupStorageList = await _lfStorageDataSource.ListAsync(StoragePriority.Backup);

            await operation.SuccessfulFinishAsync(result.ToJson(indented: true));
            return result;
        }

        public async Task AddStorageAsync(IACISOperation operation, AddLFStorageMessage message)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await operation.LogInfoAsync("Retrieving existing LogForwarder storage account meta data from DB ...");
            var primaryList = await _lfStorageDataSource.ListAsync(StoragePriority.Primary);
            var backupList = await _lfStorageDataSource.ListAsync(StoragePriority.Backup);

            if (primaryList.Any(s => s.AccountName.OrdinalEquals(message.AccountName)) ||
                backupList.Any(s => s.AccountName.OrdinalEquals(message.AccountName)))
            {
                var msg = $"The storage account '{message.AccountName}' already exist in the storage list. Skip adding it.";
                await operation.LogInfoAsync(msg);
                await operation.SuccessfulFinishAsync(msg);
                return;
            }

            var storageEntity = new StorageEntity()
            {
                AccountName = message.AccountName,
                ResourceId = message.ResourceId,
                LogForwarderRegion = message.LogForwarderRegion,
                StorageRegion = message.StorageRegion,
                Priority = message.Priority,
            };

            await operation.LogInfoAsync("Adding new storage account into meta data DB ...");
            await _lfStorageDataSource.AddAsync(storageEntity);
            await operation.SuccessfulFinishAsync($"Successfully added storage account '{message.AccountName}'. Please make sure the blob container, queue are created. Also the RBAC roles are assigned.");
        }
    }
}
