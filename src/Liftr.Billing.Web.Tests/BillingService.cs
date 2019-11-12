//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Liftr.Logging.AspNetCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    internal class BillingService : IDisposable
    {
        public readonly TestBillingServiceProvider BillingServiceProvider;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public BillingService()
        {
            _server = new TestServer(CreateWebHostBuilder());
            _client = _server.CreateClient();
            BillingServiceProvider = new TestBillingServiceProvider();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
         _client.SendAsync(request);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _server.Dispose();
                _client.Dispose();
            }
        }

        private IWebHostBuilder CreateWebHostBuilder()
        {
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "test_appsettings.json");

            var builder = WebHost.CreateDefaultBuilder()
                          .ConfigureAppConfiguration((context, conf) =>
                          {
                              conf.AddJsonFile(configPath);
                          })
                          .ConfigureTestServices(services =>
                          {
                              services.AddSingleton(c => BillingServiceProvider.Object);
                          })
                          .UseLiftrLogger()
                          .UseStartup<Startup>();

            return builder;
        }
    }
}
