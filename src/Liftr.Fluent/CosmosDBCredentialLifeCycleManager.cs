//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Liftr.Contracts;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public class CosmosDBCredentialLifeCycleManager : CredentialLifeCycleManager
    {
        private ICosmosDBAccount _db;

        public CosmosDBCredentialLifeCycleManager(ICosmosDBAccount db, ITimeSource timeSource, ILogger logger, int rotateAfterDays = 28)
            : base(timeSource, logger, rotateAfterDays)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<string> GetActiveConnectionStringAsync(bool readOnly = false, bool skipRotate = false)
        {
            using var ops = _logger.StartTimedOperation(nameof(GetActiveConnectionStringAsync));
            ops.SetContextProperty("cosmosDdId", _db.Id);

            try
            {
                var activeCredential = await CheckAndRotateAsync(forceRotate: false, skipRotate: skipRotate);
                var keys = await _db.GetConnectionStringsAsync();

                if (activeCredential == ActiveCredentialType.Primary)
                {
                    return readOnly ? keys.PrimaryReadOnlyMongoDBConnectionString : keys.PrimaryMongoDBConnectionString;
                }
                else
                {
                    return readOnly ? keys.SecondaryReadOnlyMongoDBConnectionString : keys.SecondaryMongoDBConnectionString;
                }
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public override async Task RotateCredentialAsync()
        {
            using var ops = _logger.StartTimedOperation(nameof(RotateCredentialAsync));
            ops.SetContextProperty("cosmosDdId", _db.Id);

            try
            {
                await CheckAndRotateAsync(forceRotate: true);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        private async Task<ActiveCredentialType> CheckAndRotateAsync(bool forceRotate, bool skipRotate = false)
        {
            var activeState = GetCurrentStateFromTags(_db.Tags);
            var lastRotationTime = activeState.LastRotationTime;
            var activeCredential = activeState.ActiveType;
            var needTagUpdate = activeState.NeedTagUpdate;

            var validTill = lastRotationTime + TimeSpan.FromDays(_rotateAfterDays);
            _logger.LogInformation("Skip the db connection string rotation flag is set to {skipRotate}.", skipRotate);
            if (!skipRotate && (forceRotate || _timeSource.UtcNow > validTill))
            {
                if (forceRotate)
                {
                    _logger.Information("Start rotating key due to force rotation.");
                }
                else
                {
                    _logger.Information("Start rotating key since last rotation is {lastRotationTime} and this passed the {rotateAfterDays} TTL.", lastRotationTime.ToZuluString(), _rotateAfterDays);
                }

                // Make sure the db is not in a transition state before rotating the keys.
                _db = await _db.WaitForUpdatingAsync();

                if (activeCredential == ActiveCredentialType.Primary)
                {
                    await _db.RegenerateKeyAsync("Secondary");
                    await _db.RegenerateKeyAsync("SecondaryReadonly");
                }
                else
                {
                    await _db.RegenerateKeyAsync("Primary");
                    await _db.RegenerateKeyAsync("PrimaryReadonly");
                }

                activeCredential = activeCredential == ActiveCredentialType.Primary ? ActiveCredentialType.Secondary : ActiveCredentialType.Primary;
                lastRotationTime = _timeSource.UtcNow;
                needTagUpdate = true;
            }

            if (needTagUpdate)
            {
                _logger.Information("Updating cosmos DB tags to mark rotation information. activeCredential: {activeCredential}, lastRotationTime: {lastRotationTime}", activeCredential.ToString(), lastRotationTime.ToZuluString());

                // Make sure the db is not in a transition state before changing the tags.
                _db = await _db.WaitForUpdatingAsync();

                _db = await _db.Update()
                    .WithTag(c_activeCredentailTypeTagName, activeCredential.ToString())
                    .WithTag(c_lastRotationTimeTagName, lastRotationTime.ToZuluString().ToBase64())
                    .ApplyAsync();
            }

            return activeCredential;
        }
    }
}
