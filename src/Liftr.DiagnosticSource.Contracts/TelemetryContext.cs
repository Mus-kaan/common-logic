//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.DiagnosticSource
{
    public class TelemetryContext
    {
        /// <summary>
        /// If this hold value, it will be automatically injected to all out-going dependency http requests as 'X-LIFTR-LOG-FILTER-OVERWRITE' header.
        /// </summary>
        public string LogFilterOverwrite { get; set; }

        /// <summary>
        /// Contains a unique ID provided by the client to identify the specific request.
        /// If two subsequent write requests (two PUTs, POSTs, or DELETEs) have the same id,
        /// the Network Controller assumes that last request is a retry and returns the same
        /// result it returned for the previous request.
        /// </summary>
        public string ClientRequestId { get; set; }

        /// <summary>
        /// Arm populates the header key "x-ms-arm-request-tracking-id" before sending
        /// the request to the respective Resource Provider.
        /// </summary>
        public string ARMRequestTrackingId { get; set; }

        /// <summary>
        /// Specifies the tracing correlation Id for the request. The resource provider
        /// must log this so that end-to-end requests can be correlated across Azure.
        /// </summary>
        public string CorrelationId { get; set; }

        public bool IsEmpty() =>
            string.IsNullOrEmpty(LogFilterOverwrite)
            && string.IsNullOrEmpty(ClientRequestId)
            && string.IsNullOrEmpty(ARMRequestTrackingId)
            && string.IsNullOrEmpty(CorrelationId);

        public static TelemetryContext GetCurrent()
        {
            var context = new TelemetryContext();

            if (!string.IsNullOrEmpty(CallContextHolder.LogFilterOverwrite.Value))
            {
                context.LogFilterOverwrite = CallContextHolder.LogFilterOverwrite.Value;
            }

            if (!string.IsNullOrEmpty(CallContextHolder.ClientRequestId.Value))
            {
                context.ClientRequestId = CallContextHolder.ClientRequestId.Value;
            }

            if (!string.IsNullOrEmpty(CallContextHolder.ARMRequestTrackingId.Value))
            {
                context.ARMRequestTrackingId = CallContextHolder.ARMRequestTrackingId.Value;
            }

            if (!string.IsNullOrEmpty(CallContextHolder.CorrelationId.Value))
            {
                context.CorrelationId = CallContextHolder.CorrelationId.Value;
            }

            return context.IsEmpty() ? null : context;
        }

        /// <summary>
        /// Get the current correlation Id if it is set, or return a new GUID.
        /// </summary>
        public static string GetOrGenerateCorrelationId(string correlationId = null)
        {
            if (string.IsNullOrEmpty(CallContextHolder.CorrelationId.Value))
            {
                if (string.IsNullOrEmpty(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString();
                }

                CallContextHolder.CorrelationId.Value = correlationId;
            }

            return CallContextHolder.CorrelationId.Value;
        }
    }
}
