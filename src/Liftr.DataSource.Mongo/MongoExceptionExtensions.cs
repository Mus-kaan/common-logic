//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public static class MongoExceptionExtensions
    {
        public static bool IsMongoDuplicatedKeyException(this Exception ex)
        {
            var e = ex as MongoWriteException;
            return e?.WriteError?.Code == 11000;
        }
    }
}
