//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Marketplace.Billing.Exceptions
{
    public class MarketplaceBillingException : Exception
    {
        public MarketplaceBillingException(string message)
            : base(message)
        {
        }

        public MarketplaceBillingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MarketplaceBillingException()
        {
        }
    }
}
