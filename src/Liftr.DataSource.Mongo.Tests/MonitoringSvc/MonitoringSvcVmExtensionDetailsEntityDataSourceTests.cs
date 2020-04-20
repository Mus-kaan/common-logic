//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo.MonitoringSvc;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public sealed class MonitoringSvcVmExtensionDetailsEntityDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<MonitoringSvcVMExtensionDetailsEntity> _collectionScope;

        public MonitoringSvcVmExtensionDetailsEntityDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<MonitoringSvcVMExtensionDetailsEntity>((db, collectionName) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                var collection = collectionFactory.CreateCollection<MonitoringSvcVMExtensionDetailsEntity>(collectionName);
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
#pragma warning restore CS0618 // Type or member is obsolete
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [SkipInOfficialBuild(skipLinux: true)]
        public async Task BasicDataSourceUsageAsync()
        {
            var ts = new MockTimeSource();
            var s = new MonitoringSvcVMExtensionDetailsEntityDataSource(_collectionScope.Collection);

            var extensionName = "mockName";
            var publisherName = "mockPublisher";
            var version = "mockVersion";
            var operatingSystem = "mockOPeratingSystem";
            var type = "mockType";
            var resourceProviderType = "Microsoft.Datadog/datadogs";

            var mockEntity = new MonitoringSvcVMExtensionDetailsEntity()
            {
                ExtensionName = extensionName,
                PublisherName = publisherName,
                Type = type,
                Version = version,
                OperatingSystem = operatingSystem,
                MonitoringSvcResourceProviderType = resourceProviderType,
            };

            await _collectionScope.Collection.InsertOneAsync(mockEntity);

            // Can retrieve with partnerSvcType.
            {
                var retrieved = await s.GetEntityAsync(mockEntity.MonitoringSvcResourceProviderType, mockEntity.OperatingSystem);

                Assert.Equal(extensionName, retrieved.ExtensionName);
                Assert.Equal(publisherName, retrieved.PublisherName);
                Assert.Equal(type, retrieved.Type);
                Assert.Equal(version, retrieved.Version);
                Assert.Equal(operatingSystem, retrieved.OperatingSystem);
                Assert.Equal(resourceProviderType, retrieved.MonitoringSvcResourceProviderType);
            }
        }
    }
}
