//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using System;

namespace Microsoft.Liftr.Marketplace.Saas
{
    internal class MarketplaceUrlHelper
    {
        public static string GetRequestPath(MarketplaceEnum fulfillmentAction, MarketplaceSubscription marketplaceSubscription = default, Guid operationId = default)
        {
            switch (fulfillmentAction)
            {
                case MarketplaceEnum.ListSubscriptions:
                    return MarketplaceConstants.FulfillmentPath;
                case MarketplaceEnum.ResolveToken:
                    return MarketplaceConstants.FulfillmentPath + "/resolve";
                case MarketplaceEnum.ActivateSubscription:
                    return MarketplaceConstants.FulfillmentPath + "/" + marketplaceSubscription + "/activate";
                case MarketplaceEnum.UpdateOperation:
                case MarketplaceEnum.GetOperation:
                    return MarketplaceConstants.FulfillmentPath + "/" + marketplaceSubscription + "/operations/" + operationId;
                case MarketplaceEnum.ListOperations:
                    return MarketplaceConstants.FulfillmentPath + "/" + marketplaceSubscription + "/operations";
                case MarketplaceEnum.BillingUsageEvent:
                    return MarketplaceConstants.BillingUsageEventPath;
                case MarketplaceEnum.BillingBatchUsageEvent:
                    return MarketplaceConstants.BillingBatchUsageEventPath;
                default:
                    return MarketplaceConstants.FulfillmentPath + "/" + marketplaceSubscription;
            }
        }
    }
}
