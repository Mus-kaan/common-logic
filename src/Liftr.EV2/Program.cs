//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
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

                logger.Information("Start generating EV2 artifacts ...");
                logger.Information("Load EV2 options file from: {InputFile}", options.InputFile);
                logger.Information("Output directory: {OutputPath}", options.OuputDir);

                using (var op = logger.StartTimedOperation("GenerateEV2Artifacts"))
                {
                    if (!File.Exists(options.InputFile))
                    {
                        var errMsg = "Cannot find the EV2 arififacts options file located at file path: " + options.InputFile;
                        logger.Error(errMsg);
                        op.FailOperation(errMsg);
                        throw new InvalidOperationException(errMsg);
                    }

                    var ev2Options = File.ReadAllText(options.InputFile).FromJson<EV2Options>();
                    ev2Options.CheckValid();

                    var generator = new EV2ArtifactsGenerator(logger);

                    generator.GenerateArtifacts(ev2Options, options.OuputDir);
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
