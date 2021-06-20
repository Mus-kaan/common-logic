using Azure.Core;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.Queue;
using MongoDB.Driver;
using Prometheus;
using System;

namespace Microsoft.Liftr.Sample.Worker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = Configuration;
            services.AddSingleton<ITimeSource, SystemTimeSource>();

            // 'DataAssetOptions' is loaded from Key Vault by default. This is set at provisioning time.
            var optionsValue = Configuration.GetSection(nameof(DataAssetOptions)).Value.FromJson<DataAssetOptions>();
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

            services.AddControllers();
            services.AddHostedService<Worker>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
    }
}
