//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.VNetInjection.DataSource.Mongo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PrivateIPAllocationMethod
    {
        Static,
        Dynamic,
    }
}
