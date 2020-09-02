//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.Liftr.Hosting.Contracts;
using System;
using System.IO;

namespace Microsoft.Liftr.EV2
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var logger = SimpleCustomLogSink.GetLogger();

            CommandLine.Parser.Default.ParseArguments<RunnerCommandOptions>(args)
                .WithParsed<RunnerCommandOptions>(opts => Start(opts, logger))
                .WithNotParsed<RunnerCommandOptions>((errs) =>
                {
                    Console.Error.WriteLine(errs);
                });
        }

        public static void Start(RunnerCommandOptions options, Serilog.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                logger.Information("Start generating EV2 rollout artifacts ...");

                if (!File.Exists(options.HostingEV2OptionsFile) && !File.Exists(options.ImageBuilderEV2OptionsFile))
                {
                    var errMsg = $"Cannot find the EV2 artifact options file located at file path: {options.HostingEV2OptionsFile}. Also cannot find the image builder options file at: {options.ImageBuilderEV2OptionsFile}.";
                    logger.Fatal(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var generator = new EV2ArtifactsGenerator(logger);

                if (File.Exists(options.HostingEV2OptionsFile))
                {
                    if (!File.Exists(options.HostingOptionsFile))
                    {
                        var errMsg = $"Cannot find the 'hosting-options.json' at path: {options.HostingOptionsFile}.";
                        logger.Fatal(errMsg);
                        throw new InvalidOperationException(errMsg);
                    }

                    logger.Information("Load EV2 options file from: {InputFile}. Hosting options from file: {hostingOptionsFile}", options.HostingEV2OptionsFile, options.HostingOptionsFile);

                    using (var op = logger.StartTimedOperation("GenerateEV2Artifacts"))
                    {
                        try
                        {
                            var ev2Options = File.ReadAllText(options.HostingEV2OptionsFile).FromJson<EV2HostingOptions>();
                            ev2Options.CheckValid();

                            var hostingOptions = File.ReadAllText(options.HostingOptionsFile).FromJson<HostingOptions>();
                            hostingOptions.CheckValid();

                            generator.GenerateArtifacts(ev2Options, hostingOptions, options.OuputDir);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, ex.Message);
                            op.FailOperation();
                            throw;
                        }
                    }
                }

                if (File.Exists(options.ImageBuilderEV2OptionsFile))
                {
                    logger.Information("Load Image Builder EV2 options file from: {ImageInputFile}", options.ImageBuilderEV2OptionsFile);

                    using (var op = logger.StartTimedOperation("GenerateEV2ImageBuilderArtifacts"))
                    {
                        try
                        {
                            var ev2Options = File.ReadAllText(options.ImageBuilderEV2OptionsFile).FromJson<EV2ImageBuilderOptions>();
                            ev2Options.CheckValid();
                            generator.GenerateImageBuilderArtifacts(ev2Options, options.OuputDir);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, ex.Message);
                            op.FailOperation();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                throw;
            }
            finally
            {
                SimpleCustomLogSink.Flush();
            }
        }
    }
}
