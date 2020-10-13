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

        // Marketplace Headers
        public const string MarketplaceRequestIdHeaderKey = "x-ms-requestid";
        public const string MarketplaceCorrelationIdHeaderKey = "x-ms-correlationid";

        // Logging Tags Used
        public const string BillingLogTag = "Billing";
        public const string SAASLogTag = "SAAS";
        public const string WebhookLogTag = "Webhook";
    }
}
