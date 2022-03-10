//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.VNetInjection.DataSource.Mongo;
using System;

namespace Microsoft.Liftr.VNetInjection.DataSource.Mongo.Hosting
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddVNetInjectionDataSource(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<VNetInjectionMongoOptions>(configuration.GetSection(nameof(VNetInjectionMongoOptions)));

            services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();
                var assetOptions = sp.GetService<DataAssetOptions>();

                if (assetOptions == null)
                {
                    var ex = new InvalidOperationException($"Please make sure {nameof(DataAssetOptions)} is set.");
                    logger.Error(ex.Message);
                    throw ex;
                }

                var mongoOptions = sp.GetService<IOptions<VNetInjectionMongoOptions>>()?.Value;

                if (mongoOptions == null)
                {
                    var ex = new InvalidOperationException($"Please make sure {nameof(VNetInjectionMongoOptions)} is set.");
                    logger.Error(ex.Message);
                    throw ex;
                }

                mongoOptions.ConnectionString = assetOptions.RegionalDBConnectionString;
                mongoOptions.CheckValid();

                logger.Information("Adding MongoCollectionsFactory for database: '{DatabaseName}' to the DI container", mongoOptions.DatabaseName);
                return new MongoCollectionsFactory(mongoOptions, logger);
            });

            services.AddSingleton<IVNetInjectionEntityDataSource, VNetInjectionEntityDataSource>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();
                var timeSource = sp.GetService<ITimeSource>();
                var mongoOptions = sp.GetService<IOptions<VNetInjectionMongoOptions>>().Value;
                var factory = sp.GetService<MongoCollectionsFactory>();

                logger.Information("Adding 'VNetInjectionEntityDataSource' for collection: '{VNetInjectionCollectionName}' to the DI container", mongoOptions.VNetInjectionCollectionName);
                var collection = factory.GetOrCreateEntityCollection<VNetInjectionEntity>(mongoOptions.VNetInjectionCollectionName);
                return new VNetInjectionEntityDataSource(collection, factory.MongoWaitQueueProtector, timeSource);
            });

            return services;
        }
    }
}
