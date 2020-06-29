//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Marketplace
{
    public sealed class SaaSClientHack
    {
        private readonly SubscriptionChecker _subscriptionChecker;

        public SaaSClientHack(SaaSClientHackOptions hackOptions)
        {
            if (hackOptions == null)
            {
                throw new ArgumentNullException(nameof(hackOptions));
            }

            var subs = new List<string>();
            if (hackOptions.IgnoringSubscriptions != null)
            {
                foreach (var sub in hackOptions.IgnoringSubscriptions)
                {
                    if (!Guid.TryParse(sub, out _))
                    {
                        throw new InvalidOperationException($"There exist invalid Guid format in {nameof(hackOptions)}: {hackOptions.ToJsonString()}");
                    }

                    subs.Add(sub);
                }
            }

            _subscriptionChecker = new SubscriptionChecker(new SubscriptionCheckerOptions() { Subscriptions = subs });
        }

        public bool ShouldIgnoreSaaSCreateFailure(string subscriptionId)
            => _subscriptionChecker.Contains(subscriptionId);
    }
}
