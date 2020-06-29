//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public sealed class SubscriptionChecker
    {
        private readonly HashSet<string> _subscriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public SubscriptionChecker(SubscriptionCheckerOptions checkerOptions)
        {
            if (checkerOptions == null)
            {
                throw new ArgumentNullException(nameof(checkerOptions));
            }

            if (checkerOptions.Subscriptions == null)
            {
                return;
            }

            foreach (var sub in checkerOptions.Subscriptions)
            {
                if (!Guid.TryParse(sub, out _))
                {
                    throw new InvalidOperationException($"There exist invalid Guid format in {nameof(checkerOptions)}: {checkerOptions.ToJsonString()}");
                }

                _subscriptions.Add(sub);
            }
        }

        public bool Contains(string subscriptionId)
            => _subscriptions.Contains(subscriptionId);
    }
}
