//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Marketplace.ARM.Exceptions
{
    public class MarketplaceARMException : Exception
    {
        public MarketplaceARMException(string message)
            : base(message)
        {
        }

        public MarketplaceARMException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MarketplaceARMException()
        {
        }
    }
}
