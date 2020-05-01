//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;
using System;

namespace Microsoft.Liftr.Contracts.Marketplace
{
    [SwaggerExtension(ExcludeFromSwagger = true)]
    public class MarketplaceSubscription
    {
        public MarketplaceSubscription(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }

        public override bool Equals(object obj)
        {
            return obj is MarketplaceSubscription subscription &&
                   Id.Equals(subscription.Id);
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public static MarketplaceSubscription From(string id)
        {
            if (!Guid.TryParse(id, out var subId))
            {
                throw new ArgumentException("Marketplace subscription is expected to be a guid");
            }

            return new MarketplaceSubscription(subId);
        }
    }
}
