//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.Mongo;
using System;

namespace Microsoft.Liftr.ManagedIdentity.DataSource.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddManagedIdentityDataSource(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<ManagedIdentityMongoOptions>(configuration.GetSection(nameof(ManagedIdentityMongoOptions)));

            services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();
                var assetOptions = sp.GetService<DataAssetOptions>();
                var mongoOptions = sp.GetService<IOptions<ManagedIdentityMongoOptions>>().Value;
                mongoOptions.ConnectionString = assetOptions.RegionalDBConnectionString;
                mongoOptions.CheckValid();
                return new MongoCollectionsFactory(mongoOptions, logger);
            });

            services.AddSingleton<IManagedIdentityEntityDataSource, ManagedIdentityEntityDataSource>((sp) =>
            {
                var timeSource = sp.GetService<ITimeSource>();
                var mongoOptions = sp.GetService<IOptions<ManagedIdentityMongoOptions>>().Value;
                var factory = sp.GetService<MongoCollectionsFactory>();
                var collection = factory.GetOrCreateEntityCollection<ManagedIdentityEntity>(mongoOptions.ManagedIdentityCollectionName);
                return new ManagedIdentityEntityDataSource(collection, factory.MongoWaitQueueProtector, timeSource);
            });

            return services;
        }
    }
}
