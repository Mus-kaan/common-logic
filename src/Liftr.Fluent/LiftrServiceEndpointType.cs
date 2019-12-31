//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Network.Fluent.Models;

namespace Microsoft.Liftr.Fluent
{
    public static class LiftrServiceEndpointType
    {
        public static readonly ServiceEndpointType MicrosoftKeyVault = ServiceEndpointType.Parse("Microsoft.KeyVault");
    }
}
