//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Marketplace.ARM.Models;

namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    public class PartnerRequestMetaData
    {
        public MarketplaceRequestMetadata MarketplaceMetadata { get; set; }

        public ManagedIdentityRequestMetadata ManagedIdentityMetadata { get; set; }
    }
}
