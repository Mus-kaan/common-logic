//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Storage.Queues;
using Liftr.Metrics.AOP;
using Liftr.Sample.WorkerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.GenericHosting;
using Microsoft.Liftr.Logging.GenericHosting;
using Microsoft.Liftr.Logging.Metrics;
using Microsoft.Liftr.Metrics;
using Microsoft.Liftr.Queue;
using MongoDB.Driver;
using Serilog;
using System;

namespace Microsoft.Liftr.Sample.WorkerService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseKeyVaultProvider(keyVaultPrefix: "SampleRP")
            .UseLiftrLogger()
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                services.AddSingleton<ITimeSource, SystemTimeSource>();

                // 'DataAssetOptions' is loaded from Key Vault by default. This is set at provisioning time.
                var optionsValue = configuration.GetSection(nameof(DataAssetOptions)).Value.FromJson<DataAssetOptions>();
                services.AddSingleton(optionsValue);

                services.Configure<MongoOptions>(configuration.GetSection(nameof(MongoOptions)));
                services.Configure<QueueReaderOptions>(configuration.GetSection(nameof(QueueReaderOptions)));

                services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
                {
                    var assetOptions = sp.GetService<DataAssetOptions>();
                    var logger = sp.GetService<Serilog.ILogger>();
                    var mongoOptions = sp.GetService<IOptions<MongoOptions>>().Value;

                    if (mongoOptions == null)
                    {
                        throw new InvalidOperationException($"Could not find {nameof(MongoOptions)} in configuration");
                    }

                    mongoOptions.ConnectionString = assetOptions.RegionalDBConnectionString;
                    return new MongoCollectionsFactory(mongoOptions, logger);
                });

                services.AddSingleton<ICounterEntityDataSource, CounterEntityDataSource>((sp) =>
                {
                    var logger = sp.GetService<Serilog.ILogger>();
                    try
                    {
                        var timeSource = sp.GetService<ITimeSource>();
                        var factory = sp.GetService<MongoCollectionsFactory>();
                        IMongoCollection<CounterEntity> collection = factory.GetOrCreateCounterEntityCollection("counter-entity");
                        return new CounterEntityDataSource(collection, factory.MongoWaitQueueProtector, timeSource);
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(ex, "cannot create ICounterEntityDataSource");
                        throw;
                    }
                });

                services.AddSingleton<IQueueReader, QueueReader>((sp) =>
                {
                    var logger = sp.GetService<Serilog.ILogger>();
                    var assetOptions = sp.GetService<DataAssetOptions>();
                    var tokenCredentials = sp.GetService<TokenCredential>();
                    var qOptions = sp.GetService<IOptions<QueueReaderOptions>>().Value;

                    var queueUri = new Uri($"https://{assetOptions.StorageAccountName}.queue.core.windows.net/sample-queue");
                    QueueClient queue = new QueueClient(queueUri, tokenCredentials);
                    queue.CreateIfNotExists();

                    return new QueueReader(queue, qOptions, sp.GetService<ITimeSource>(), logger);
                });

                services.AddSingleton<IMetricSender>((sp) =>
                {
                    return new MetricSender("localhost", "SampleAppMetrics", Log.Logger, null);
                });
                services.AddSingletonWithProxy<ITestClass, TestClass>();
                var serviceProvider = services.BuildServiceProvider();
                var testProxy = serviceProvider.GetService<ITestClass>();
                testProxy.TestMethod();
                testProxy.TestMethodAsync();
                testProxy.TestMethodWithExceptionAsync();
                services.AddHostedService<Worker>();
            });
    }
}
