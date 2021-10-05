//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Hosting.Swagger;
using Microsoft.Liftr.RPaaS;
using Microsoft.Liftr.RPaaS.Hosting;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;

namespace SampleWebApp
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
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
            services.AddControllersWithViews();
            services.AddControllers();

            services.AddHttpClient(Options.DefaultName)
                .AddPolicyHandler(GetRetryPolicy());

            services.Configure<MetaRPOptions>(Configuration.GetSection(nameof(MetaRPOptions)));
            services.Configure<MetaRPOptions>((metaRPOptions) =>
            {
                metaRPOptions.FPAOptions.KeyVaultEndpoint = new Uri(Configuration[GlobalSettingConstants.VaultEndpoint]);
            });

            services.AddMetaRPClientWithTokenProvider(Configuration);

            // Register the Swagger generator, defining 1 or more Swagger documents
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

            services.AddSwaggerGenNewtonsoftSupport();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                // We need to serialize the swagger using version 2 as ARM does not support version 3.
                c.SerializeAsV2 = true;
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            // Warm dependency up
#pragma warning disable CA1062 // Validate arguments of public methods
            app.ApplicationServices.GetService<IMetaRPStorageClient>();
#pragma warning restore CA1062 // Validate arguments of public methods
        }

        private static Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> GetRetryPolicy()
        {
            // https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            return
                (services, request) =>
                    HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                            delay,
                            onRetry: (outcome, timespan, retryAttempt, context) =>
                            {
                                var logger = services.GetService<ILogger>();
                                logger.Warning("Request: {requestMethod} {requestUrl} failed. Delaying for {delay}ms, then retrying {retry}.", request.Method, request.RequestUri, timespan.TotalMilliseconds, retryAttempt);
                            });
        }
    }
}
