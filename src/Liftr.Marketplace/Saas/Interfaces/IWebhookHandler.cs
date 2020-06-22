//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Microsoft.Liftr.Marketplace.Saas.Contracts;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Marketplace.Saas.Interfaces
{
    public interface IWebhookHandler
    {
        Task<OperationUpdateStatus> ProcessDeleteAsync(MarketplaceSubscription marketplaceSubscription);
    }
}