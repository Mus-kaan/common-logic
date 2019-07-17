//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Rest;
using Serilog;
using System;
using System.IO;

namespace Microsoft.Liftr.Provisioning.Runner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Minute)
                .CreateLogger();

            if (args.Length != 1)
            {
                logger.Fatal($"Invalid usage. Please use: {System.AppDomain.CurrentDomain.FriendlyName} [environment config json file path]");
                return 1;
            }

            var configPath = args[0];

            if (!File.Exists(configPath))
            {
                logger.Fatal($"Config json file doesn't exist at the path: {configPath}");
                return 1;
            }

            var authFile = "my.azureauth";
            if (!File.Exists(authFile))
            {
                logger.Fatal($"There is no auth file at the path: {configPath}");
                logger.Fatal("Please follow this page to generate the auth file: https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md");
                logger.Fatal("az ad sp create-for-rbac --sdk-auth > my.azureauth");
                return 1;
            }

            try
            {
                var options = InfraV1Options.FromFile(configPath);

                ServiceClientTracing.AddTracingInterceptor(new TracingInterceptor(logger));
                ServiceClientTracing.IsEnabled = true;

                var credentials = SdkContext.AzureCredentialsFactory.FromFile(authFile);
                var authContract = AuthFileContact.FromFile(authFile);

                var azure = Azure.Management.Fluent.Azure
                    .Configure()
                    .WithDelegatingHandler(new HttpLoggingDelegatingHandler())
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(credentials.DefaultSubscriptionId);

                logger.Information("Selected subscription: " + azure.SubscriptionId);

                var client = new AzureClient(azure, authContract.ClientId, authContract.ClientSecret, logger);
                var infra = new InftrastructureV1(client, logger);

                // This will take a long time. Be patient.
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                infra.CreateDataAndComputeAsync(options).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }

            return 0;
        }
    }
}
