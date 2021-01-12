//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DiagnosticSource
{
    public static class HeaderConstants
    {
        /// <summary>
        /// Contains a unique ID provided by the client to identify the specific request.
        /// If two subsequent write requests (two PUTs, POSTs, or DELETEs) have the same id,
        /// the Network Controller assumes that last request is a retry and returns the same
        /// result it returned for the previous request.
        /// </summary>
        public const string ARMClientRequestId = "X-MS-Client-Request-Id";
        public const string LiftrClientRequestId = "X-Liftr-Client-Request-Id";

        /// <summary>
        /// Arm populates the header key "x-ms-arm-request-tracking-id" before sending
        /// the request to the respective Resource Provider.
        /// https://stackoverflow.microsoft.com/questions/151637/what-header-key-will-be-unique-when-receiving-request-from-arm
        /// </summary>
        public const string ARMRequestTrackingId = "X-MS-Arm-Request-Tracking-Id";
        public const string LiftrARMRequestTrackingId = "X-Liftr-Arm-Request-Tracking-Id";

        /// <summary>
        /// Specifies the tracing correlation Id for the request. The resource provider
        /// must log this so that end-to-end requests can be correlated across Azure.
        /// https://docs.microsoft.com/en-us/rest/api/datafactory/v1/data-factory-gateway
        /// </summary>
        public const string RequestCorrelationId = "X-MS-Correlation-Request-Id";
        public const string LiftrRequestCorrelationId = "X-Liftr-Correlation-Request-Id";

        public const string LiftrLogLevelOverwrite = "X-Liftr-Log-Filter-Overwrite";

        public const string MarketplaceRequestId = "x-ms-requestId";

        public const string MarketplaceCorrelationId = "x-ms-correlationid";
    }
}
