//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DBService.Contracts
{
    public enum ProvisioningState
    {
        Accepted,
        Creating,
        Updating,
        Deleting,
        Succeeded,
        Failed,
        Canceled,
        Deleted,
        NotSpecified,
    }

    public static class ProvisioningStateExtensions
    {
        public static bool IsFinalState(this ProvisioningState state)
            => state >= ProvisioningState.Succeeded;
    }
}