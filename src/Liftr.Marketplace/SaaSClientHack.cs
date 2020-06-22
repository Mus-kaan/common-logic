//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Marketplace
{
    public sealed class SaaSClientHack
    {
        private readonly HashSet<string> _ignoreSubscriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public SaaSClientHack(SaaSClientHackOptions hackOptions)
        {
            if (hackOptions == null)
            {
                throw new ArgumentNullException(nameof(hackOptions));
            }

            if (hackOptions.IgnoringSubscriptions == null)
            {
                return;
            }

            foreach (var sub in hackOptions.IgnoringSubscriptions)
            {
                if (!Guid.TryParse(sub, out _))
                {
                    throw new InvalidOperationException($"There exist invalid Guid format in {nameof(hackOptions)}: {hackOptions.ToJsonString()}");
                }

                _ignoreSubscriptions.Add(sub);
            }
        }

        public bool ShouldIgnoreSaaSCreateFailure(string subscriptionId)
            => _ignoreSubscriptions.Contains(subscriptionId);
    }
}
