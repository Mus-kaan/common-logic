//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr
{
    public class UpdateConflictException : Exception
    {
        public UpdateConflictException()
        {
        }

        public UpdateConflictException(string message)
            : base(message)
        {
        }

        public UpdateConflictException(Exception innerException)
            : base(innerException?.Message ?? throw new ArgumentNullException(nameof(innerException)), innerException)
        {
        }

        public UpdateConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
