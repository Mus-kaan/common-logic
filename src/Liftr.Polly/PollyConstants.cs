//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Polly
{
    public static class PollyConstants
    {
        public const int MarketplaceRetryForEntityNotFoundCount = 2;
        public const string MarketplaceRetryForEntityNotFoundLogTag = "MarketplaceRetryForEntityNotFound";
        public const int MarketplaceRetryCount = 5;
        public const string MarketplaceRetryLogTag = "MarketplaceRetry";
        public const int CacheRefreshRetryWait = 5;
    }
}
