//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Liftr.Logging
{
    public static class LoggerExtensions
    {
        public static void LogInformation<T>(this ILogger logger, string messageTemplate, T propertyValue, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            logger.Information(EnrichMessage(messageTemplate, filePath, memberName, lineNumber), propertyValue);
        }

        public static void LogInformation(this ILogger logger, string messageTemplate, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            logger.Information(EnrichMessage(messageTemplate, filePath, memberName, lineNumber));
        }

        public static void LogVerbose<T>(this ILogger logger, string messageTemplate, T propertyValue, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            logger.Verbose(EnrichMessage(messageTemplate, filePath, memberName, lineNumber), propertyValue);
        }

        public static void LogVerbose(this ILogger logger, string messageTemplate, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            logger.Verbose(EnrichMessage(messageTemplate, filePath, memberName, lineNumber));
        }

        private static string EnrichMessage(string messageTemplate, string filePath, string memberName, int lineNumber)
        {
            return $"[{Path.GetFileName(filePath)}:{memberName}:{lineNumber}] {messageTemplate}";
        }
    }
}
