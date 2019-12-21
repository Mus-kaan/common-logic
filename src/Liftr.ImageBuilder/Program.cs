//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.GenericHosting;
using Microsoft.Liftr.Logging.GenericHosting;
using Serilog.Context;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Liftr.ImageBuilder
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var settingsContent = EmbeddedContentReader.GetContent(Assembly.GetExecutingAssembly(), "Microsoft.Liftr.ImageBuilder.embedded-appsettings.json");
                File.WriteAllText("embedded-appsettings.json", settingsContent);

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

            CommandLine.Parser.Default.ParseArguments<BuilderCommandOptions>(args)
                .WithParsed<BuilderCommandOptions>(opts => StartHost(opts))
                .WithNotParsed<BuilderCommandOptions>((errs) =>
                {
                    Console.Error.WriteLine(errs);
                });
        }

        public static void StartHost(BuilderCommandOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
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
                    services.AddSingleton<BuilderCommandOptions>(options);

                    services.AddSingleton<ITimeSource, SystemTimeSource>();

                    services.AddHostedService<ActionExecutor>();
                })
                .UseConsoleLifetime()
                .Build();

            host.Run();
        }
    }
}
