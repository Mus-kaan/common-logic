//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Liftr.Logging;
using Serilog.Events;
using System;

namespace Microsoft.Liftr.Configuration
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    public static class ConfigurationExtensions
    {
        public static bool ContainsSerilogWriteToConsole(this IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            try
            {
                var writeTos = configuration.GetSection("Serilog")?.GetSection("WriteTo")?.GetChildren();
                if (writeTos == null)
                {
                    return false;
                }

                foreach (var writeTo in writeTos)
                {
                    var name = writeTo.GetSection("Name")?.Value;
                    if (name.OrdinalContains("Console"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static LiftrLoggingOptions ExtractLoggingOptions(this IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var options = new LiftrLoggingOptions()
            {
                WriteToConsole = configuration.ContainsSerilogWriteToConsole(),
                RenderMessage = ExtractValue(configuration, nameof(LiftrLoggingOptions.RenderMessage), defaultValue: false),
                LogTimedOperation = ExtractValue(configuration, nameof(LiftrLoggingOptions.LogTimedOperation), defaultValue: true),
                LogRequest = ExtractValue(configuration, nameof(LiftrLoggingOptions.LogRequest), defaultValue: false),
                LogHostName = ExtractValue(configuration, nameof(LiftrLoggingOptions.LogHostName), defaultValue: false),
                LogSubdomain = ExtractValue(configuration, nameof(LiftrLoggingOptions.LogSubdomain), defaultValue: false),
                AllowFilterDynamicOverride = ExtractValue(configuration, nameof(LiftrLoggingOptions.AllowFilterDynamicOverride), defaultValue: false),
                AllowDestructureUsingAttributes = ExtractValue(configuration, nameof(LiftrLoggingOptions.AllowDestructureUsingAttributes), defaultValue: false),
                MinimumLevel = ExtractMinimumLevel(configuration),
                AppInsigthsInstrumentationKey = ExtractAppInsightsKey(configuration),
            };

            return options;
        }

        private static bool ExtractValue(IConfiguration configuration, string propertyName, bool defaultValue = false)
        {
            try
            {
                var valueStr = configuration.GetSection("Serilog")?.GetSection(propertyName)?.Value;
                if (string.IsNullOrEmpty(valueStr))
                {
                    return defaultValue;
                }

                return bool.Parse(valueStr);
            }
            catch
            {
            }

            return defaultValue;
        }

        private static LogEventLevel ExtractMinimumLevel(IConfiguration configuration)
        {
            LogEventLevel defaultLevel = LogEventLevel.Information;
            try
            {
                var defaultLevelStr = configuration.GetSection("Serilog")?.GetSection("MinimumLevel")?.GetSection("Default")?.Value;
                defaultLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), defaultLevelStr);
            }
            catch
            {
            }

            return defaultLevel;
        }

        private static string ExtractAppInsightsKey(IConfiguration configuration)
        {
            return configuration.GetSection("ApplicationInsights")?.GetSection("InstrumentationKey")?.Value;
        }
    }
}
