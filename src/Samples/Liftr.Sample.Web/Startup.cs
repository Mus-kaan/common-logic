//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Storage.Queues;
using Liftr.MarketplaceResource.DataSource;
using Liftr.MarketplaceResource.DataSource.Interfaces;
using Liftr.MarketplaceResource.DataSource.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.Hosting.Swagger;
using Microsoft.Liftr.Queue;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Liftr.Sample.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly Serilog.ILogger _logger;

        public Startup(IConfiguration configuration, Serilog.ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogProcessStart();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITimeSource, SystemTimeSource>();

            // 'RPAssetOptions' is loaded from Key Vault by default. This is set at provisioning time.
            var optionsValue = _configuration.GetSection(nameof(RPAssetOptions)).Value.FromJson<RPAssetOptions>();
            services.AddSingleton(optionsValue);

            services.AddHttpClient();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                c.EnableAnnotations();
                c.OperationFilter<DefaultResponseOperationFilter>();
                c.OperationFilter<RPSwaggerOperationFilter>();
                c.SchemaFilter<RPSwaggerSchemaFilter>();
                c.DocumentFilter<RPSwaggerDocumentFilter>();

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.Configure<MongoOptions>(_configuration.GetSection(nameof(MongoOptions)));

            services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
            {
                var assetOptions = sp.GetService<RPAssetOptions>();
                var mongoOptions = sp.GetService<IOptions<MongoOptions>>().Value;

                if (mongoOptions == null)
                {
                    throw new InvalidOperationException($"Could not find {nameof(MongoOptions)} in configuration");
                }

                mongoOptions.ConnectionString = assetOptions.CosmosDBConnectionStrings.Where(i => i.Description.OrdinalEquals(assetOptions.ActiveKeyName)).FirstOrDefault().ConnectionString;
                return new MongoCollectionsFactory(mongoOptions, _logger);
            });

            services.AddSingleton<ICounterEntityDataSource, CounterEntityDataSource>((sp) =>
            {
                try
                {
                    var timeSource = sp.GetService<ITimeSource>();
                    var factory = sp.GetService<MongoCollectionsFactory>();
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                    IMongoCollection<CounterEntity> collection = factory.GetOrCreateCounterEntityCollectionAsync("counter-entity").Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                    return new CounterEntityDataSource(collection, timeSource);
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex, "cannot create ICounterEntityDataSource");
                    throw;
                }
            });

            services.AddSingleton<IMarketplaceResourceEntityDataSource, MarketplaceResourceEntityDataSource>((sp) =>
            {
                var logger = sp.GetService<ILogger>();

                try
                {
                    var timeSource = sp.GetService<ITimeSource>();
                    var factory = sp.GetService<MongoCollectionsFactory>();
#pragma warning disable Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result
                    IMongoCollection<MarketplaceResourceEntity> collection = factory.GetOrCreateEntityCollectionAsync<MarketplaceResourceEntity>("resource-metadata-entity").Result;
#pragma warning restore Liftr1004 // Avoid calling System.Threading.Tasks.Task<TResult>.Result

                    var marketplaceSubscriptionIdx = new CreateIndexModel<MarketplaceResourceEntity>(Builders<MarketplaceResourceEntity>.IndexKeys.Ascending(item => item.MarketplaceSubscription), new CreateIndexOptions<MarketplaceResourceEntity> { Unique = false });
                    collection.Indexes.CreateOne(marketplaceSubscriptionIdx);

                    return new MarketplaceResourceEntityDataSource(collection, timeSource);
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "Cannot create IResourceMetadataEntityDataSource");
                    throw;
                }
            });

            services.AddSingleton<IQueueWriter, QueueWriter>((sp) =>
            {
                var assetOptions = sp.GetService<RPAssetOptions>();
                var tokenCredentials = sp.GetService<TokenCredential>();

                var queueUri = new Uri($"https://{assetOptions.StorageAccountName}.queue.core.windows.net/sample-queue");
                QueueClient queue = new QueueClient(queueUri, tokenCredentials);
                queue.CreateIfNotExists();

                return new QueueWriter(queue, sp.GetService<ITimeSource>(), _logger);
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddControllers();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            // Warm dependency up
            app.ApplicationServices.GetService<ICounterEntityDataSource>();
            app.ApplicationServices.GetService<IQueueWriter>();
        }
    }
}
