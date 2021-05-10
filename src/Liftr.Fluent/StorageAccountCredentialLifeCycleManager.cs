//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Liftr.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class StorageAccountCredentialLifeCycleManager : CredentialLifeCycleManager
    {
        private IStorageAccount _storageAccount;

        public StorageAccountCredentialLifeCycleManager(IStorageAccount storageAccount, ITimeSource timeSource, Serilog.ILogger logger, int rotateAfterDays = 28)
            : base(timeSource, logger, rotateAfterDays)
        {
            _storageAccount = storageAccount ?? throw new ArgumentNullException(nameof(storageAccount));
        }

        public async Task<string> GetActiveConnectionStringAsync()
        {
            var key = await GetActiveKeyAsync();
            return key.ToConnectionString(_storageAccount.Name);
        }

        public async Task<StorageAccountKey> GetActiveKeyAsync()
        {
            var activeCredential = await CheckAndRotateAsync(forceRotate: false);
            var activeKeyName = activeCredential == ActiveCredentialType.Primary ? "key1" : "key2";

            var key = (await _storageAccount.GetKeysAsync()).Where(k => k.KeyName.OrdinalEquals(activeKeyName)).FirstOrDefault();
            return key;
        }

        public override Task RotateCredentialAsync()
        {
            return CheckAndRotateAsync(forceRotate: true);
        }

        private async Task<ActiveCredentialType> CheckAndRotateAsync(bool forceRotate)
        {
            var activeState = GetCurrentStateFromTags(_storageAccount.Tags);
            var lastRotationTime = activeState.LastRotationTime;
            var activeCredential = activeState.ActiveType;
            var needTagUpdate = activeState.NeedTagUpdate;

            var validTill = lastRotationTime + TimeSpan.FromDays(_rotateAfterDays);
            if (forceRotate || _timeSource.UtcNow > validTill)
            {
                var rotatingKeyName = activeCredential == ActiveCredentialType.Primary ? "key2" : "key1";
                if (forceRotate)
                {
                    _logger.Information("Start rotating '{keyName}' due to force rotation.", rotatingKeyName);
                }
                else
                {
                    _logger.Information("Start rotating '{keyName}', since last rotation is {lastRotationTime} and this passed the {rotateAfterDays} TTL.", rotatingKeyName, lastRotationTime.ToZuluString(), _rotateAfterDays);
                }

                await _storageAccount.RegenerateKeyAsync(rotatingKeyName);
                activeCredential = activeCredential == ActiveCredentialType.Primary ? ActiveCredentialType.Secondary : ActiveCredentialType.Primary;
                lastRotationTime = _timeSource.UtcNow;
                needTagUpdate = true;
            }

            if (needTagUpdate)
            {
                _logger.Information("Updating storage account tags to mark rotation information. activeCredential: {activeCredential}, lastRotationTime: {lastRotationTime}", activeCredential.ToString(), lastRotationTime.ToZuluString());
                _storageAccount = await _storageAccount.Update()
                    .WithTag(c_activeCredentailTypeTagName, activeCredential.ToString())
                    .WithTag(c_lastRotationTimeTagName, lastRotationTime.ToZuluString().ToBase64())
                    .ApplyAsync();
            }

            return activeCredential;
        }
    }
}
