//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class CollectionNotExistException : Exception
    {
        public CollectionNotExistException()
        {
        }

        public CollectionNotExistException(string message)
            : base(message)
        {
        }

        public CollectionNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
