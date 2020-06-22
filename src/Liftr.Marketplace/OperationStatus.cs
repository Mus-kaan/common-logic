//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Marketplace.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationStatus
    {
        NotStarted,
        InProgress,
        Failed,
        Succeeded,
        Conflict,
    }
}
