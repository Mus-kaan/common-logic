//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The Type that holds all the policies for a rollout target.
    /// </summary>
    public class Policy
    {
        /// <summary>
        /// Gets or sets the flag that determines if safe rollout policy should be enforced.
        /// </summary>
        public bool SkipSafeRolloutPolicyCheck { get; set; }
    }
}
