//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace;
using Microsoft.Liftr.Marketplace.Agreement.Interfaces;
using Microsoft.Liftr.Marketplace.ARM.Interfaces;
using Microsoft.Liftr.Marketplace.Saas.Interfaces;

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class MarketplaceBuilder
    {
        public IMarketplaceARMClient MarketplaceARMClient { get; set; }

        public IMarketplaceFulfillmentClient MarketplaceFulfillmentClient { get; set; }

        public ISignAgreementService SignAgreementService { get; set; }

        public SaaSClientHack SaaSClientHack { get; set; }
    }
}
