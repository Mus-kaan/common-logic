//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Metrics.DiagnosticSource
{
    public static class MetricConstants
    {
        public const string LiftrMetricTypeHeaderKey = "x-ms-metrictype";
        public const string MarketplaceMetricType = "marketplace";
        public const string MetaRPMetricType = "metarp";
        public const string PartnerMetricType = "partner";

        public const string HTTPVerb_Marketplace_Duration = "HTTPVerb_Marketplace_Duration";
        public const string HTTPVerb_Marketplace_Result = "HTTPVerb_Marketplace_Result";

        public const string HTTPVerb_MetaRP_Duration = "HTTPVerb_MetaRP_Duration";
        public const string HTTPVerb_MetaRP_Result = "HTTPVerb_MetaRP_Result";

        public const string HTTPVerb_PartnerAPI_Duration = "HTTPVerb_PartnerAPI_Duration";
        public const string HTTPVerb_PartnerAPI_Result = "HTTPVerb_PartnerAPI_Result";

        public const string HTTPVerb_DefaultCalls_Duration = "HTTPVerb_Outgoing_Duration";
        public const string HTTPVerb_DefaultCalls_Result = "HTTPVerb_Outgoing_Result";
    }
}
