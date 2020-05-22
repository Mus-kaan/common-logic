﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
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
    }

    public static class ProvisioningStateExtensions
    {
        public static bool IsFinalState(this ProvisioningState state)
            => state >= ProvisioningState.Succeeded;
    }
}
