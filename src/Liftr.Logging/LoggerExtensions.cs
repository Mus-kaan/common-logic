//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
using Serilog;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Liftr
{
    public static class LoggerExtensions
    {
        public static LiftrLoggingOptions Options { get; } = new LiftrLoggingOptions();

        /// <summary>
        /// Start tracking an <see cref="ITimedOperation"/>, which contains two log events:
        /// <para> 1. The 'start' event will be logged when call this function.</para>
        /// <para> 2. The 'finish' event will be logged when the <see cref="ITimedOperation"/> is disposed.</para>
        /// The 'start' and 'finish' event will contain some common proerpties like 'success', 'duration'.
        /// You can add more proerties using <see cref="ITimedOperation.SetProperty(string,string)"/> or <see cref="ITimedOperation.SetContextProperty(string,string)"/>
        /// </summary>
        /// <returns>The <see cref="ITimedOperation"/> object</returns>
        public static ITimedOperation StartTimedOperation(this ILogger logger, string operationName, string operationId = null, bool generateMetrics = false, bool newCorrelationId = false)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new TimedOperation(logger, operationName, operationId, generateMetrics, newCorrelationId);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>")]
        public static void LogProcessStart(this ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var meta = InstanceMetaHelper.GetMetaInfoAsync().Result;
            var instanceMeta = meta.InstanceMeta;

            logger.Information("**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********");

            logger.Information("Process start. Assembly info: {assemblyName}, version: {assemblyProductVersion}, machine name: {machineName}, liftrLibraryVersion: {liftrLibraryVersion}", meta.AssemblyName, meta.Version, Environment.MachineName, meta.LiftrLibraryVersion);

            if (instanceMeta != null && instanceMeta.Compute != null)
            {
                string vmLocation = instanceMeta.Compute.Location;
                string vmName = instanceMeta.Compute.Name;
                string vmSize = instanceMeta.Compute.VmSize;
                logger.Information("Process start Azure Compute Info: vmLocation: {vmLocation}, vmName: {vmName}, vmSize: {vmSize}", vmLocation, vmName, vmSize);
            }

            logger.Information("**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********[https://aka.ms/liftr]**********[Liftr]**********");
        }

        public static void LogError<T>(this ILogger logger, string messageTemplate, T propertyValue, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Error(EnrichMessage(messageTemplate, filePath, memberName, lineNumber), propertyValue);
        }

        public static void LogError(this ILogger logger, string messageTemplate, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Error(EnrichMessage(messageTemplate, filePath, memberName, lineNumber));
        }

        public static void LogInformation<T>(this ILogger logger, string messageTemplate, T propertyValue, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Information(EnrichMessage(messageTemplate, filePath, memberName, lineNumber), propertyValue);
        }

        public static void LogInformation(this ILogger logger, string messageTemplate, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Information(EnrichMessage(messageTemplate, filePath, memberName, lineNumber));
        }

        public static void LogVerbose<T>(this ILogger logger, string messageTemplate, T propertyValue, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Verbose(EnrichMessage(messageTemplate, filePath, memberName, lineNumber), propertyValue);
        }

        public static void LogVerbose(this ILogger logger, string messageTemplate, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Verbose(EnrichMessage(messageTemplate, filePath, memberName, lineNumber));
        }

        private static string EnrichMessage(string messageTemplate, string filePath, string memberName, int lineNumber)
        {
            return $"[{Path.GetFileName(filePath)}:{memberName}:{lineNumber}] {messageTemplate}";
        }
    }
}
