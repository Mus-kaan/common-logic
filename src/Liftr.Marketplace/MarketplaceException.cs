//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public class MarketplaceException : Exception
    {
        public MarketplaceException(string message)
             : base(message)
        {
        }

        public MarketplaceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MarketplaceException(string message, MarketplaceHttpException marketplaceHttpException)
            : base(message)
        {
            MarketplaceHttpException = marketplaceHttpException;
        }

        public MarketplaceException()
        {
        }

        public MarketplaceHttpException MarketplaceHttpException { get; }
    }
}