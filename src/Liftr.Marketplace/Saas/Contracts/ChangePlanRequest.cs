//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace.Saas.Contracts
{
    public class ChangePlanRequest
    {
        public ChangePlanRequest(string planId)
        {
            if (string.IsNullOrEmpty(planId))
            {
                throw new System.ArgumentException($"'{nameof(planId)}' cannot be null or empty.", nameof(planId));
            }

            PlanId = planId;
        }

        public string PlanId { get; }
    }
}
