//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.ManagedIdentity.DataSource
{
    public static class AsyncCursorExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursor<T> asyncCursor)
        {
            if (asyncCursor == null)
            {
                throw new ArgumentNullException(nameof(asyncCursor));
            }

            while (await asyncCursor.MoveNextAsync())
            {
                foreach (var current in asyncCursor.Current)
                {
                    yield return current;
                }
            }
        }
    }
}
