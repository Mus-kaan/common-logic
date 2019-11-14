//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Liftr.Billing.Web
{
    public class BatchUsageEvent
    {
       public IEnumerable<UsageEvent> UsageEvents { get; set; }
    }
}
