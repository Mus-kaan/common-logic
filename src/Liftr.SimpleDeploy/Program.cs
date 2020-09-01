//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.DiagnosticSource;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.GenericHosting;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Liftr.Logging.GenericHosting;
using Serilog.Context;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Liftr.SimpleDeploy
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var settingsContent = EmbeddedContentReader.GetContent(Assembly.GetExecutingAssembly(), "Microsoft.Liftr.SimpleDeploy.embedded-appsettings.json");
                File.WriteAllText("embedded-appsettings.json", settingsContent);

                var rollOutId = Environment.GetEnvironmentVariable("RolloutId");
                if (!string.IsNullOrEmpty(rollOutId))
                {
                    LogContext.PushProperty("EV2RolloutId", rollOutId);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }

            try
            {
                var ev2CorrelationId = Environment.GetEnvironmentVariable("CorrelationId");
                if (!string.IsNullOrEmpty(ev2CorrelationId))
                {
                    LogContext.PushProperty("EV2CorrelationId", ev2CorrelationId);
                    TelemetryContext.GetOrGenerateCorrelationId(ev2CorrelationId);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
            }

            try
            {
                CommandLine.Parser.Default.ParseArguments<RunnerCommandOptions>(args)
                .WithParsed<RunnerCommandOptions>(opts => StartHost(opts))
                .WithNotParsed<RunnerCommandOptions>((errs) =>
                {
                    Console.Error.WriteLine(errs);
                });
            }
            catch (OperationCanceledException)
            {
                // swallow operation cancelled operation.
            }
        }

        public static void StartHost(RunnerCommandOptions options)
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
                    services.AddSingleton<RunnerCommandOptions>(options);

                    services.AddSingleton<HostingOptions>((sp) =>
                    {
                        var logger = sp.GetService<Serilog.ILogger>();
                        try
                        {
                            var hostingOptions = File.ReadAllText(options.ConfigPath).FromJson<HostingOptions>();
                            return hostingOptions;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Failed at configuring HostingOptions");
                            throw;
                        }
                    });

                    services.AddHostedService<ActionExecutor>();
                })
                .UseConsoleLifetime()
                .Build();

            host.Run();
        }
    }
}
