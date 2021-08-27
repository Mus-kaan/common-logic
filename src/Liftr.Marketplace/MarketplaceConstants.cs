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
        public const string TokenRequestPath = "OAuth2/marketplace/token";
        public const string CertificateRequestPath = "OAuth2/marketplace/certificate";
        public const int PollingCount = 40;
        public const int DefaultTTL = 10;
        public const int SemaphoreInitialThreadCount = 1;
        public const int SemaphoreMaxThreadCount = 1;

        // Marketplace Headers
        public const string MarketplaceRequestIdHeaderKey = "x-ms-requestid";
        public const string MarketplaceCorrelationIdHeaderKey = "x-ms-correlationid";

        // Logging Tags Used
        public const string BillingLogTag = "Billing";
        public const string SAASLogTag = "SAAS";
        public const string WebhookLogTag = "Webhook";
        internal const string AsyncOperationLocation = "Operation-Location";
        internal const string DefaultApiVersionParameterName = "api-version";
    }
}
