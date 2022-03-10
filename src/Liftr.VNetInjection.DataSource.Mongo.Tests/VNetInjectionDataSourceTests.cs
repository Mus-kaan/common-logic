//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Liftr;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.DataSource.Mongo.Tests.Common;
using Microsoft.Liftr.Logging;
using Microsoft.Liftr.VNetInjection.DataSource.Mongo;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Liftr.MarketplaceRelationship.DataSource.Test
{
    public sealed class VNetInjectionDataSourceTests : IDisposable
    {
        private readonly TestCollectionScope<VNetInjectionEntity> _collectionScope;
        private readonly MockTimeSource _ts = new MockTimeSource();

        public VNetInjectionDataSourceTests()
        {
            var option = new MockMongoOptions() { ConnectionString = TestDBConnection.TestMongodbConStr, DatabaseName = TestDBConnection.TestDatabaseName };
            var collectionFactory = new MongoCollectionsFactory(option, LoggerFactory.VoidLogger);
            _collectionScope = new TestCollectionScope<VNetInjectionEntity>((db, collectionName) =>
            {
                var collection = collectionFactory.GetOrCreateEntityCollection<VNetInjectionEntity>(collectionName);
                return collection;
            });
        }

        public void Dispose()
        {
            _collectionScope.Dispose();
        }

        [CheckInValidation(skipLinux: true)]
        public async Task AddVNetInjectionEntityAsync()
        {
            using var rateLimiter = new MongoWaitQueueRateLimiter(100, TestLogger.VoidLogger);
            var dataSource = new VNetInjectionEntityDataSource(_collectionScope.Collection, rateLimiter, _ts);

            var subnetResourceId = "/subscriptions/9e35ddcc-b909-4eb5-bef3-38547a6eebb7/resourceGroups/project/providers/Microsoft.Network/virtualNetworks/project-vnet/subnets/default";
            var publicIPResourceId = "/subscriptions/9e35ddcc-b909-4eb5-bef3-38547a6eebb7/resourceGroups/project/providers/Microsoft.Network/publicIPAddresses/testvm-ip";
            var resourceId1 = "/subscriptions/9e35ddcc-b909-4eb5-bef3-38547a6eebb7/resourceGroups/project/providers/NGINX.NGINXPLUS/nginxDeployments/test";

            var entity = new VNetInjectionEntity()
            {
                ResourceId = resourceId1,
                NetworkInterfaceConfiguration = new NetworkInterfaceConfiguration()
                {
                    DelegatedSubnetResourceIds = new List<string>()
                    {
                        subnetResourceId,
                    },
                },
                FrontendIPConfiguration = new FrontendIPConfiguration()
                {
                    PublicIPResourceIds = new List<string>()
                    {
                        publicIPResourceId,
                    },
                },
                ManagedResourceGroupName = "ngx_managed_rg",
            };

            var entity1 = await dataSource.UpsertAsync(entity);

            // Can retrieve single entity.
            {
                var retrieved = await dataSource.GetAsync(entity1.EntityId);

                subnetResourceId.ToUpperInvariant().Should().BeEquivalentTo(retrieved.NetworkInterfaceConfiguration.DelegatedSubnetResourceIds.First());
                publicIPResourceId.ToUpperInvariant().Should().BeEquivalentTo(retrieved.FrontendIPConfiguration.PublicIPResourceIds.First());
                resourceId1.ToUpperInvariant().Should().BeEquivalentTo(retrieved.ResourceId);

                var exceptedStr = entity1.ToJson();
                var actualStr = retrieved.ToJson();
                Assert.Equal(exceptedStr, actualStr);
            }
        }
    }
}
