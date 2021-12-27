//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.Liftr.IdempotentRPWorker.Contracts
{
    /// <summary>
    /// Marketplace States that can be processed by the RP worker.
    /// </summary>
    public enum MarketplaceStatesEnum
    {
        CreateSaaS,

        ActivateSaaS,

        AddMarketplaceRelationshipEntity,

        LinkMarketplace,

        DeleteSaaS,

        DeleteMarketplaceRelationshipEntity,
    }
}