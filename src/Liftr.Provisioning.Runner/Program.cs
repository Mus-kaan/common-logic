//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
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
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
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
                if (string.IsNullOrEmpty(credentials.DefaultSubscriptionId))
                {
                    throw new InvalidOperationException("Cannot find default subscription Id in the credential file");
                }

                logger.Information("Selected subscription: {@SubscriptionId}", credentials.DefaultSubscriptionId);

                var authContract = AuthFileContact.FromFile(authFile);
                var factory = new LiftrAzureFactory(credentials, credentials.DefaultSubscriptionId, logger);
                var client = factory.GenerateLiftrAzure();

                var infra = new InftrastructureV1(client, logger);

                // This will take a long time. Be patient.
#pragma warning disable Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
                infra.CreateDataAndComputeAsync(options).Wait();
#pragma warning restore Liftr1005 // Avoid calling System.Threading.Tasks.Task.Wait()
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                logger.Error(ex, ex.Message);
            }

            return 0;
        }
    }
}
