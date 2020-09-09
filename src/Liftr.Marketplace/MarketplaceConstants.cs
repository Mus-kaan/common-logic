//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace
{
    public static class MarketplaceConstants
    {
        public const string BillingUsageEventPath = "api/usageEvent";
        public const string BillingBatchUsageEventPath = "api/batchUsageEvent";
        public const string FulfillmentPath = "api/saas/subscriptions";
        public const string MarketplaceCreateSAASPath = "api/saasresources/subscriptions";

        // Billing Headers
        public const string BillingRequestIdHeaderKey = "x-ms-requestid";
        public const string BillingCorrelationIdHeaderKey = "x-ms-correlationid";

        // Logging Tags Used
        public const string BillingLogTag = "Billing";
        public const string SAASLogTag = "SAAS";
    }
}
