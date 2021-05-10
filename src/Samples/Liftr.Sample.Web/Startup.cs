//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.DataSource.Mongo;
using Microsoft.Liftr.Hosting.Swagger;
using Microsoft.Liftr.Marketplace.Saas;
using Microsoft.Liftr.MarketplaceResource.DataSource;
using Microsoft.Liftr.Profiler.AspNetCore;
using Microsoft.Liftr.Queue;
using Microsoft.Liftr.TokenManager;
using Microsoft.Liftr.TokenManager.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Prometheus;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Liftr.Sample.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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

            services.AddSingleton<ITimeSource, SystemTimeSource>();

            // 'RPAssetOptions' is loaded from Key Vault by default. This is set at provisioning time.
            var optionsValue = _configuration.GetSection(nameof(RPAssetOptions)).Value.FromJson<RPAssetOptions>();
            if (optionsValue != null)
            {
                services.AddSingleton(optionsValue);
            }

            services.AddHttpClient();

            services.Configure<MongoOptions>(_configuration.GetSection(nameof(MongoOptions)));
            services.Configure<MongoOptions>(_configuration.GetSection(nameof(MongoOptions)));

            services.Configure<SingleTenantAADAppTokenProviderOptions>(_configuration.GetSection("SampleFPA"));
            services.Configure<SingleTenantAADAppTokenProviderOptions>((ops) =>
            {
                ops.KeyVaultEndpoint = new Uri(_configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();
                var assetOptions = sp.GetService<RPAssetOptions>();
                var mongoOptions = sp.GetService<IOptions<MongoOptions>>().Value;

                if (mongoOptions == null)
                {
                    throw new InvalidOperationException($"Could not find {nameof(MongoOptions)} in configuration");
                }

                mongoOptions.ConnectionString = assetOptions.CosmosDBConnectionStrings.Where(i => i.Description.OrdinalEquals(assetOptions.ActiveKeyName)).FirstOrDefault().ConnectionString;
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

            services.AddSingleton<IMarketplaceSaasResourceDataSource, MarketplaceSaasResourceDataSource>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();

                try
                {
                    var timeSource = sp.GetService<ITimeSource>();
                    var factory = sp.GetService<MongoCollectionsFactory>();
                    var collection = factory.GetOrCreateMarketplaceEntityCollection("resource-metadata-entity");

                    return new MarketplaceSaasResourceDataSource(collection, factory.MongoWaitQueueProtector, timeSource);
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "Cannot create IResourceMetadataEntityDataSource");
                    throw;
                }
            });

            services.AddSingleton<IQueueWriter, QueueWriter>((sp) =>
            {
                var logger = sp.GetService<Serilog.ILogger>();
                var assetOptions = sp.GetService<RPAssetOptions>();
                var tokenCredentials = sp.GetService<TokenCredential>();

                var queueUri = new Uri($"https://{assetOptions.StorageAccountName}.queue.core.windows.net/sample-queue");
                QueueClient queue = new QueueClient(queueUri, tokenCredentials);
                queue.CreateIfNotExists();

                return new QueueWriter(queue, sp.GetService<ITimeSource>(), logger, messageVisibilityTimeout: TimeSpan.FromSeconds(5));
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSingleton<IMultiTenantAppTokenProvider, MultiTenantAppTokenProvider>((sp) =>
            {
                var options = sp.GetService<IOptions<SingleTenantAADAppTokenProviderOptions>>().Value;
                var kvClient = sp.GetService<IKeyVaultClient>();
                var logger = sp.GetService<Serilog.ILogger>();

                return new MultiTenantAppTokenProvider(options, kvClient, logger);
            });

            services.AddSingleton<ISingleTenantAppTokenProvider, SingleTenantAppTokenProvider>((sp) =>
            {
                var options = sp.GetService<IOptions<SingleTenantAADAppTokenProviderOptions>>().Value;
                var kvClient = sp.GetService<IKeyVaultClient>();
                var logger = sp.GetService<Serilog.ILogger>();

                return new SingleTenantAppTokenProvider(options, kvClient, logger);
            });

            services.AddMarketplaceARMClient(_configuration);

            services.AddControllers();
            services.AddRazorPages();

            services.AddAppInsightsProfiler(_configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var logger = app.ApplicationServices.GetService<Serilog.ILogger>();
            logger.LogProcessStart();

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
            app.UseHttpMetrics(options =>
            {
                // This identifies the page when using Razor Pages.
                options.AddRouteParameter("page");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapMetrics();
            });

            // Warm dependency up
            app.ApplicationServices.GetService<ICounterEntityDataSource>();
            app.ApplicationServices.GetService<IQueueWriter>();
            app.ApplicationServices.GetService<IMultiTenantAppTokenProvider>();
            app.ApplicationServices.GetService<ISingleTenantAppTokenProvider>();
        }
    }
}
