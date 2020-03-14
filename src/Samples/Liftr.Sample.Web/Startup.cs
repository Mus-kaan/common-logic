//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Liftr;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.DataSource.Mongo;
using MongoDB.Driver;
using System;
using System.Linq;

namespace Liftr.Sample.Web
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

            services.AddHttpClient();

            services.Configure<RPAssetOptions>((rpAssets) =>
            {
                string rpAssetString = _configuration[nameof(RPAssetOptions)];
                if (!string.IsNullOrEmpty(rpAssetString))
                {
                    var newAssets = rpAssetString.FromJson<RPAssetOptions>();
                    rpAssets.ActiveKeyName = newAssets.ActiveKeyName;
                    rpAssets.StorageAccountName = newAssets.StorageAccountName;
                    rpAssets.CosmosDBConnectionStrings = newAssets.CosmosDBConnectionStrings;
                    rpAssets.DataPlaneSubscriptions = newAssets.DataPlaneSubscriptions;
                }
                else
                {
                    var errMsg = $"Cannot load the content of '{nameof(RPAssetOptions)}' from configuration.";
                    _logger.Warning(errMsg);
                }
            });

            services.AddSingleton<MongoCollectionsFactory, MongoCollectionsFactory>((sp) =>
            {
                var assetOptions = sp.GetService<IOptions<RPAssetOptions>>().Value;
                MongoOptions mongoOptions = new MongoOptions();
                mongoOptions.DatabaseName = "test-db";
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

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
