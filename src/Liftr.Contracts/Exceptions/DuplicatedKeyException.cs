//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr
{
    public class DuplicatedKeyException : Exception
    {
        public DuplicatedKeyException()
        {
        }

        public DuplicatedKeyException(string message)
            : base(message)
        {
        }

        public DuplicatedKeyException(Exception innerException)
            : base(innerException?.Message ?? throw new ArgumentNullException(nameof(innerException)), innerException)
        {
        }

        public DuplicatedKeyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
