//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Constants
{
    public static class MPConstants
    {
        public const string MonthlyBillingTermType = "P1M";
        public const string MonthlyOfferTermId = "hjdtn7tfnxcy";
        public const string YearlyOfferTermId = "nm5o4wf9fbfy";
        public const string FakeSaaSResource = "FAKE_MP_RESOURCE";
        public const string EntityNotFound = "EntityNotFound";
        public const double MarketplaceRetryWaitForEntityNotFound = 60.0;
        public const string SAASLogTag = "SAAS";
    }
}
