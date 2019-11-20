//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.GenericHosting;
using Microsoft.Liftr.Logging.GenericHosting;
using Serilog.Context;
using System;
using System.IO;

namespace Microsoft.Liftr.SimpleDeploy
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var rollOutId = Environment.GetEnvironmentVariable("RolloutId");
                if (!string.IsNullOrEmpty(rollOutId))
                {
                    Console.WriteLine("Current EV2 roll out Id: " + rollOutId);
                    LogContext.PushProperty("EV2RolloutId ", rollOutId);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }

            CommandLine.Parser.Default.ParseArguments<RunnerCommandOptions>(args)
                .WithParsed<RunnerCommandOptions>(opts => StartHost(opts))
                .WithNotParsed<RunnerCommandOptions>((errs) =>
                {
                    Console.Error.WriteLine(errs);
                });
        }

        public static void StartHost(RunnerCommandOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.SubscriptionId))
            {
                throw new InvalidOperationException("Please sepcify a valid Subscription Id.");
            }

            if (!File.Exists(options.ConfigPath))
            {
                var errMsg = $"Config json file doesn't exist at the path: {options.ConfigPath}";
                Console.Error.WriteLine(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            var host = new HostBuilder()
                .UseDefaultAppConfig()
                .UseLiftrLogger()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<EnvironmentOptions>(hostContext.Configuration.GetSection(nameof(EnvironmentOptions)));

                    services.AddSingleton<RunnerCommandOptions>(options);

                    services.AddHostedService<ActionExecutor>();
                })
                .UseConsoleLifetime()
                .Build();

            host.Run();
        }
    }
}
