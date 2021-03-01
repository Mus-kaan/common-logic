//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Microsoft.Liftr.DiagnosticSource")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.AspNetCore.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Logging.GenericHosting")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Queue")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Common")]
[assembly: InternalsVisibleTo("Microsoft.Liftr.Tests.Utilities")]

namespace Microsoft.Liftr.DiagnosticSource
{
    internal static class CallContextHolder
    {
        /// <summary>
        /// If this hold value, it will be automatically injected to all out-going dependency http requests as 'X-LIFTR-LOG-FILTER-OVERWRITE' header.
        /// </summary>
        public static readonly AsyncLocal<string> LogFilterOverwrite = new AsyncLocal<string>();

        /// <summary>
        /// Contains a unique ID provided by the client to identify the specific request.
        /// If two subsequent write requests (two PUTs, POSTs, or DELETEs) have the same id,
        /// the Network Controller assumes that last request is a retry and returns the same
        /// result it returned for the previous request.
        /// </summary>
        public static readonly AsyncLocal<string> ClientRequestId = new AsyncLocal<string>();

        /// <summary>
        /// Arm populates the header key "x-ms-arm-request-tracking-id" before sending
        /// the request to the respective Resource Provider.
        /// </summary>
        public static readonly AsyncLocal<string> ARMRequestTrackingId = new AsyncLocal<string>(); // https://stackoverflow.microsoft.com/questions/151637/what-header-key-will-be-unique-when-receiving-request-from-arm

        /// <summary>
        /// Specifies the tracing correlation Id for the request. The resource provider
        /// must log this so that end-to-end requests can be correlated across Azure.
        /// </summary>
        public static readonly AsyncLocal<string> CorrelationId = new AsyncLocal<string>(); // https://docs.microsoft.com/en-us/rest/api/datafactory/v1/data-factory-gateway

        public static readonly AsyncLocal<string> ARMOperationName = new AsyncLocal<string>();
    }
}
