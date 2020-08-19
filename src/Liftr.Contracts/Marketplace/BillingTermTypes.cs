//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts.Marketplace
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BillingTermTypes
    {
        Monthly,
        Yearly,
    }
}