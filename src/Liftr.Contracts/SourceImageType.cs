//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SourceImageType
    {
        WindowsServer2016Datacenter,
        WindowsServer2016DatacenterCore,
        WindowsServer2016DatacenterContainers,
        WindowsServer2019Datacenter,
        WindowsServer2019DatacenterCore,
        WindowsServer2019DatacenterContainers,
        U1604LTS,
        U1804LTS,
    }
}
