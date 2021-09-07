//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using System;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders
{
    public static class MemoryCacheExtensions
    {
        public static T Set<T>(this IMemoryCache cache, object key, T value, long size, TimeSpan expiry)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = expiry;
            entry.Value = value;
            entry.Size = size;

            if (value is IDisposable)
            {
                entry.RegisterPostEvictionCallback(
                    (key, value, reason, substate) =>
                    {
                        (value as IDisposable)?.Dispose();
                    });
            }

            entry.Dispose();

            return value;
        }
    }
}
