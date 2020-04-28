//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Logging;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Liftr
{
    public static class LoggerExtensions
    {
        private static MetaInfo s_metaInfo;
        private static bool s_metaInitialized;
        private static List<IDisposable> s_disposablesHolder = new List<IDisposable>();

        /// <summary>
        /// Start tracking an <see cref="ITimedOperation"/>, which contains two log events:
        /// <para> 1. The 'start' event will be logged when call this function.</para>
        /// <para> 2. The 'finish' event will be logged when the <see cref="ITimedOperation"/> is disposed.</para>
        /// The 'start' and 'finish' event will contain some common proerpties like 'success', 'duration'.
        /// You can add more proerties using <see cref="ITimedOperation.SetProperty(string,string)"/> or <see cref="ITimedOperation.SetContextProperty(string,string)"/>
        /// </summary>
        /// <returns>The <see cref="ITimedOperation"/> object</returns>
        public static ITimedOperation StartTimedOperation(this ILogger logger, string operationName, string operationId = null)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new TimedOperation(logger, operationName, operationId);
        }

        public static async Task<MetaInfo> GetMetaInfoAsync(this ILogger logger, Assembly callingAssembly = null)
        {
            if (!s_metaInitialized)
            {
                s_metaInitialized = true;

                if (callingAssembly == null)
                {
                    callingAssembly = Assembly.GetEntryAssembly();
                }

                var assemblyName = callingAssembly.GetName().Name;
                var assemblyProductVersion = FileVersionInfo.GetVersionInfo(callingAssembly.Location).ProductVersion;
                s_disposablesHolder.Add(LogContext.PushProperty("AssemblyName", assemblyName));
                s_disposablesHolder.Add(LogContext.PushProperty("AssemblyVersion", assemblyProductVersion));

                var instanceMeta = await InstanceMetadata.LoadAsync(logger);

                if (instanceMeta != null)
                {
                    s_disposablesHolder.Add(LogContext.PushProperty(nameof(instanceMeta.MachineNameEnv), instanceMeta.MachineNameEnv));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(instanceMeta.Compute.AzEnvironment), instanceMeta.Compute.AzEnvironment));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(instanceMeta.Compute.Location), instanceMeta.Compute.Location));
                    s_disposablesHolder.Add(LogContext.PushProperty("MetaVM" + nameof(instanceMeta.Compute.Name), instanceMeta.Compute.Name));
                    s_disposablesHolder.Add(LogContext.PushProperty("MetaResourceGroup", instanceMeta.Compute.ResourceGroupName));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(instanceMeta.Compute.Sku), instanceMeta.Compute.Sku));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(instanceMeta.Compute.VmSize), instanceMeta.Compute.VmSize));
                }

                s_metaInfo = new MetaInfo()
                {
                    InstanceMeta = instanceMeta,
                    AssemblyName = assemblyName,
                    Version = assemblyProductVersion,
                };
            }

            return s_metaInfo;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>")]
        public static void LogProcessStart(this ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var meta = logger.GetMetaInfoAsync(Assembly.GetEntryAssembly()).Result;
            var instanceMeta = meta.InstanceMeta;

            logger.Information("***********************************************************************************");

            logger.Information("Process start. Assembly info: {assemblyName}, version: {assemblyProductVersion}, machine name: {machineName}", meta.AssemblyName, meta.Version, Environment.MachineName);

            if (instanceMeta != null && instanceMeta.Compute != null)
            {
                string vmLocation = instanceMeta.Compute.Location;
                string vmName = instanceMeta.Compute.Name;
                string vmSize = instanceMeta.Compute.VmSize;
                logger.Information("Process start Azure Compute Info: vmLocation: {vmLocation}, vmName: {vmName}, vmSize: {vmSize}", vmLocation, vmName, vmSize);
            }

            logger.Information("***********************************************************************************");
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
