//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public enum IPCategory
    {
        // IP facing incoming traffic. Public IP on nginx-ingress Load Balancer
        Inbound,

        // IP for outgoing traffic
        Outbound,

        // IP which can be used both as Inbound and Outbound
        InOutbound,
    }
}
