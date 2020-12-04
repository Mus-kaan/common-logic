//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Marketplace.Exceptions
{
    public class MarketplaceTerminalException : MarketplaceException
    {
        public MarketplaceTerminalException(string message)
            : base(message)
        {
        }

        public MarketplaceTerminalException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MarketplaceTerminalException()
        {
        }
    }
}
