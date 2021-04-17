//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Net.Http.Headers;

namespace Microsoft.Liftr.Utilities
{
    public static class HttpHeadersExtensions
    {
        /// <summary>
        /// Adds if not exists the specified header and its value into the System.Net.Http.Headers.HttpHeaders collection.
        /// </summary>
        /// <param name="headers">http headers</param>
        /// <param name="name">The header to add to the collection.</param>
        /// <param name="value">The content of the header.</param>
        public static void AddIfNotExists(this HttpHeaders headers, string name, string value)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (headers.Contains(name))
            {
                return;
            }

            headers.Add(name, value);
        }
    }
}
