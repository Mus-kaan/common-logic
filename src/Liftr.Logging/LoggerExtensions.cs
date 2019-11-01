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

namespace Microsoft.Liftr
{
    public static class LoggerExtensions
    {
        private static bool s_metaInitialized;
        private static bool s_assemblyInfoInitialized;
        private static InstanceMetadata s_instanceMeta;
        private static List<IDisposable> s_disposablesHolder = new List<IDisposable>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1004:Avoid calling System.Threading.Tasks.Task<TResult>.Result", Justification = "<Pending>")]
        public static InstanceMetadata GetInstanceMetadata(this ILogger logger)
        {
            if (!s_metaInitialized)
            {
                s_metaInitialized = true;
                s_instanceMeta = InstanceMetadata.LoadAsync(logger).Result;

                if (s_instanceMeta != null)
                {
                    s_disposablesHolder.Add(LogContext.PushProperty(nameof(s_instanceMeta.MachineNameEnv), s_instanceMeta.MachineNameEnv));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(s_instanceMeta.Compute.AzEnvironment), s_instanceMeta.Compute.AzEnvironment));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(s_instanceMeta.Compute.Location), s_instanceMeta.Compute.Location));
                    s_disposablesHolder.Add(LogContext.PushProperty("MetaVM" + nameof(s_instanceMeta.Compute.Name), s_instanceMeta.Compute.Name));
                    s_disposablesHolder.Add(LogContext.PushProperty("MetaResourceGroup", s_instanceMeta.Compute.ResourceGroupName));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(s_instanceMeta.Compute.Sku), s_instanceMeta.Compute.Sku));
                    s_disposablesHolder.Add(LogContext.PushProperty("Meta" + nameof(s_instanceMeta.Compute.VmSize), s_instanceMeta.Compute.VmSize));
                }
            }

            return s_instanceMeta;
        }

        public static ITimedOperation StartTimedOperation(this ILogger logger, string operationName, string operationId = null)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new TimedOperation(logger, operationName, operationId);
        }

        public static void LogProcessStart(this ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var callingAssembly = Assembly.GetCallingAssembly();
            var assemblyName = callingAssembly.GetName().Name;
            var assemblyProductVersion = FileVersionInfo.GetVersionInfo(callingAssembly.Location).ProductVersion;

            if (!s_assemblyInfoInitialized)
            {
                s_assemblyInfoInitialized = true;

                s_disposablesHolder.Add(LogContext.PushProperty("AssemblyName", assemblyName));
                s_disposablesHolder.Add(LogContext.PushProperty("AssemblyVersion", assemblyProductVersion));
            }

            var meta = logger.GetInstanceMetadata();

            logger.Information("***********************************************************************************");

            logger.Information("Process start. Assembly info: {assemblyName}, version: {assemblyProductVersion}, machine name: {machineName}", assemblyName, assemblyProductVersion, Environment.MachineName);

            if (meta != null && meta.Compute != null)
            {
                string vmLocation = meta.Compute.Location;
                string vmName = meta.Compute.Name;
                string vmSize = meta.Compute.VmSize;
                logger.Information("Process start Azure Compute Info: vmLocation: {vmLocation}, vmName: {vmName}, vmSize: {vmSize}", vmLocation, vmName, vmSize);
            }

            logger.Information("***********************************************************************************");
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
