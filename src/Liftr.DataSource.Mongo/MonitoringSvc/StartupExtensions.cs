//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;
using Serilog;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public static class StartupExtensions
    {
        public static void UseMonitoringSvcDataSources(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MonitoringSvcMongoOptions>(configuration.GetSection("Mongo"));
            services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
            {
                var options = sp.GetService<IOptions<MonitoringSvcMongoOptions>>().Value;
                return new MongoCollectionsFactory(options, logger);
            });

            services.AddSingleton<IMonitoringSvcEventHubEntityDataSource, MonitoringSvcEventHubEntityDataSource>((sp) =>
            {
                var options = sp.GetService<IOptions<MonitoringSvcMongoOptions>>().Value;
                var factory = sp.GetService<MongoCollectionsFactory>();
                IMongoCollection<MonitoringSvcEventHubEntity> collection = null;
                try
                {
                    collection = factory.GetCollection<MonitoringSvcEventHubEntity>(options.EventHubSourceEntityCollectionName);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, "Collection doesn't exist.");
                    throw;
                }

                return new MonitoringSvcEventHubEntityDataSource(collection);
            });
            services.AddSingleton<IMonitoringSvcVMExtensionDetailsEntityDataSource, MonitoringSvcVMExtensionDetailsEntityDataSource>((sp) =>
            {
                var options = sp.GetService<IOptions<MonitoringSvcMongoOptions>>().Value;
                var factory = sp.GetService<MongoCollectionsFactory>();
                IMongoCollection<MonitoringSvcVMExtensionDetailsEntity> collection = null;
                try
                {
                    collection = factory.GetCollection<MonitoringSvcVMExtensionDetailsEntity>(options.VmExtensionDetailsEntityCollectionName);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, "Collection doesn't exist.");
                    throw;
                }

                return new MonitoringSvcVMExtensionDetailsEntityDataSource(collection);
            });
            services.AddSingleton<IMonitoringSvcMonitoredEntityDataSource, MonitoringSvcMonitoredEntityDataSource>((sp) =>
            {
                var options = sp.GetService<IOptions<MonitoringSvcMongoOptions>>().Value;
                var factory = sp.GetService<MongoCollectionsFactory>();
                IMongoCollection<MonitoringSvcMonitoredEntity> collection = null;
                try
                {
                    collection = factory.GetCollection<MonitoringSvcMonitoredEntity>(options.MonitoredEntityCollectionName);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error(ex, "Collection doesn't exist.");
                    throw;
                }

                return new MonitoringSvcMonitoredEntityDataSource(collection);
            });
        }
    }
}
