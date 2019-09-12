//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.AzureAsyncOperation;
using Microsoft.Liftr.AzureAsyncOperation.DataSource;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public sealed class AsyncOperationDataSourceTest : IDisposable
    {
        private readonly IAsyncOperationStatusDataSource _asyncOperationStatusDatSource;
        private readonly TestCollectionScope<AsyncOperationStatusEntity> _scopedCollection;

        public AsyncOperationDataSourceTest()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _scopedCollection = new TestCollectionScope<AsyncOperationStatusEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.CreateCollection<AsyncOperationStatusEntity>(collectionName);
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
#pragma warning restore CS0618 // Type or member is obsolete
                return collection;
            });
            _asyncOperationStatusDatSource = new AsyncOperationStatusDataSource(_scopedCollection.Collection);
        }

        public void Dispose()
        {
            _scopedCollection.Dispose();
        }

        [Fact]
        public async Task BasicDataSourceUsageAsync()
        {
            // Initialise dummy input
            var status = OperationStatus.Created;
            var timeout = TimeSpan.FromHours(5);
            var errorCode = "ErrorCode";
            var errMessage = "ErrorMessage";

            // Create entity test
            var entity1 = await _asyncOperationStatusDatSource.CreateAsync(
                status,
                timeout,
                errorCode,
                errMessage);
            Assert.Equal(status, entity1.Resource.Status);
            Assert.Equal(timeout, entity1.Timeout);
            Assert.Equal(errorCode, entity1.Resource.Error.Code);
            Assert.Equal(errMessage, entity1.Resource.Error.Message);

            // Retrieve entity test
            var retrieved = await _asyncOperationStatusDatSource.GetAsync(entity1.Resource.OperationId);
            Assert.Equal(status, retrieved.Resource.Status);
            Assert.Equal(timeout, retrieved.Timeout);
            Assert.Equal(errorCode, retrieved.Resource.Error.Code);
            Assert.Equal(errMessage, retrieved.Resource.Error.Message);

            // Update entity test
            var updated = await _asyncOperationStatusDatSource.UpdateAsync(
                entity1.Resource.OperationId,
                OperationStatus.Failed,
                errorCode: "Timeout",
                errorMessage: "Operation Timeout");
            retrieved = await _asyncOperationStatusDatSource.GetAsync(updated.Resource.OperationId);
            Assert.Equal(OperationStatus.Failed, retrieved.Resource.Status);
            Assert.Equal(timeout, updated.Timeout);
            Assert.Equal("Timeout", retrieved.Resource.Error.Code);
            Assert.Equal("Operation Timeout", retrieved.Resource.Error.Message);
        }
    }
}
