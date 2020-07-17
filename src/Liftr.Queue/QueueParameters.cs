//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Queue
{
    internal static class QueueParameters
    {
        /// <summary>
        /// Specifies the new visibility timeout value, in seconds, relative to
        /// server time. The value must be larger than or equal to 0, and cannot be larger
        /// than 7 days. The visibility timeout of a message cannot be set to a value later
        /// than the expiry time. A message can be updated until it has been deleted or has
        /// expired.
        /// </summary>
        public static TimeSpan VisibilityTimeout => TimeSpan.FromSeconds(40);

        /// <summary>
        /// Need to make sure during <see cref="VisibilityTimeout"/> there can be at lease 3 retries.
        /// </summary>
        public static TimeSpan MessageLeaseRenewInterval => TimeSpan.FromSeconds(10);

        public static TimeSpan ScanMaxWaitTime => TimeSpan.FromSeconds(30);

        public static TimeSpan ScanMinWaitTime => TimeSpan.FromMilliseconds(71);
    }
}
