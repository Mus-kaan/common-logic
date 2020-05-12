//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.AAD
{
    public class InvalidAADTokenException : Exception
    {
        public InvalidAADTokenException()
        {
        }

        public InvalidAADTokenException(string message)
            : base(message)
        {
        }

        public InvalidAADTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
