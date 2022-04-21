//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Data.Tables;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Liftr.ACIS.Common;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Tests.Common;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.ACIS.Relay.Tests
{
    public class RelayCoordinationTest
    {
        private readonly ITestOutputHelper _output;

        public RelayCoordinationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task TestSuccessfulOperationAsync()
        {
            using var scope = new TestResourceGroupScope(SdkContext.RandomResourceName("acis", 15), _output);
            try
            {
                var stor = await scope.GetTestStorageAccountAsync();
                ACISOperationStorageOptions options = new ACISOperationStorageOptions()
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    StorageAccountConnectionString = await stor.GetPrimaryConnectionStringAsync(),
#pragma warning restore CS0618 // Type or member is obsolete
                };

                var serviceClient = new TableServiceClient(options.StorageAccountConnectionString);
                var tableClient = serviceClient.GetTableClient(options.OperationStatusTableName);
                await tableClient.CreateIfNotExistsAsync();

                var dataSource = new ACISOperationStatusEntityDataSource(tableClient);

                // Create queue.
                {
                    Azure.Storage.CloudStorageAccount classicStorageAccount = Azure.Storage.CloudStorageAccount.Parse(options.StorageAccountConnectionString);
                    CloudQueueClient queueClient = classicStorageAccount.CreateCloudQueueClient();
                    var classicQueue = queueClient.GetQueueReference(options.OperationNotificationQueueName);
                    await classicQueue.CreateIfNotExistsAsync();
                }

                var mockACISLogger = new MockAcisLogger(scope.Logger);
                var ts = new MockTimeSource();
                ts.Add(TimeSpan.FromDays(321));

                var operationName = "ListEventHubs";
                var operationId = $"{operationName}-{ts.UtcNow.ToString("MMddHmmss", CultureInfo.InvariantCulture)}-{Guid.NewGuid()}";

                ACISWorkCoordinator coordinator = new ACISWorkCoordinator(options, ts, mockACISLogger);
                var coordinatorTask = coordinator.StartWorkAsync(operationName, parameters: string.Empty, operationId: operationId);

                ts.Add(TimeSpan.FromSeconds(5));
                var entity = await dataSource.GetEntityAsync(operationName, operationId);
                var acisOperation = new ACISOperation(entity, dataSource, ts);

                await acisOperation.LogInfoAsync("Start processing ...");

                ts.Add(TimeSpan.FromSeconds(15));
                await Task.Delay(TimeSpan.FromSeconds(15));

                Assert.Equal(3, mockACISLogger.InfoLogs.Count);
                Assert.Contains("Start processing ...", mockACISLogger.InfoLogs.Last(), StringComparison.OrdinalIgnoreCase);

                await acisOperation.SuccessfulFinishAsync("SucceededResult");
                ts.Add(TimeSpan.FromSeconds(15));
                await Task.Delay(TimeSpan.FromSeconds(15));

                var result = await coordinatorTask;
                Assert.True(result.Succeeded);
                Assert.Equal("SucceededResult", result.Result);

                // The status record will be deleted.
                entity = await dataSource.GetEntityAsync(operationName, operationId);
                Assert.Null(entity);
            }
            catch (Exception ex)
            {
                scope.SkipDeleteResourceGroup = true;
                scope.Logger.Error(ex, "Failed.");
                scope.TimedOperation.FailOperation(ex.Message);
                throw;
            }
        }

        [CheckInValidation(skipLinux: true)]
        public async Task TestFailureOperationAsync()
        {
            using var scope = new TestResourceGroupScope(SdkContext.RandomResourceName("acis", 15), _output);
            try
            {
                var stor = await scope.GetTestStorageAccountAsync();
                ACISOperationStorageOptions options = new ACISOperationStorageOptions()
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    StorageAccountConnectionString = await stor.GetPrimaryConnectionStringAsync(),
#pragma warning restore CS0618 // Type or member is obsolete
                };

                var serviceClient = new TableServiceClient(options.StorageAccountConnectionString);
                var tableClient = serviceClient.GetTableClient(options.OperationStatusTableName);
                await tableClient.CreateIfNotExistsAsync();

                var dataSource = new ACISOperationStatusEntityDataSource(tableClient);

                // Create queue.
                {
                    Azure.Storage.CloudStorageAccount classicStorageAccount = Azure.Storage.CloudStorageAccount.Parse(options.StorageAccountConnectionString);
                    CloudQueueClient queueClient = classicStorageAccount.CreateCloudQueueClient();
                    var classicQueue = queueClient.GetQueueReference(options.OperationNotificationQueueName);
                    await classicQueue.CreateIfNotExistsAsync();
                }

                var mockACISLogger = new MockAcisLogger(scope.Logger);
                var ts = new MockTimeSource();
                ts.Add(TimeSpan.FromDays(321));

                var operationName = "ListEventHubs";
                var operationId = $"{operationName}-{ts.UtcNow.ToString("MMddHmmss", CultureInfo.InvariantCulture)}-{Guid.NewGuid()}";

                ACISWorkCoordinator coordinator = new ACISWorkCoordinator(options, ts, mockACISLogger);
                var coordinatorTask = coordinator.StartWorkAsync(operationName, parameters: string.Empty, operationId: operationId);

                ts.Add(TimeSpan.FromSeconds(5));
                var entity = await dataSource.GetEntityAsync(operationName, operationId);
                var acisOperation = new ACISOperation(entity, dataSource, ts);

                await acisOperation.LogInfoAsync("Start processing ...");

                ts.Add(TimeSpan.FromSeconds(15));
                await Task.Delay(TimeSpan.FromSeconds(15));

                Assert.Equal(3, mockACISLogger.InfoLogs.Count);
                Assert.Contains("Start processing ...", mockACISLogger.InfoLogs.Last(), StringComparison.OrdinalIgnoreCase);

                await acisOperation.FailAsync("FailResult");
                ts.Add(TimeSpan.FromSeconds(15));
                await Task.Delay(TimeSpan.FromSeconds(15));

                var result = await coordinatorTask;
                Assert.False(result.Succeeded);
                Assert.Equal("FailResult", result.Result);

                // The status record will be deleted.
                entity = await dataSource.GetEntityAsync(operationName, operationId);
                Assert.NotNull(entity);
            }
            catch (Exception ex)
            {
                scope.SkipDeleteResourceGroup = true;
                scope.Logger.Error(ex, "Failed.");
                scope.TimedOperation.FailOperation(ex.Message);
                throw;
            }
        }
    }
}
