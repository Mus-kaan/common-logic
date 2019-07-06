//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Liftr.Contracts
{
    public interface IEntityId
    {
        string SubscriptionId { get; set; }

        string ResourceGroup { get; set; }

        string ResourceName { get; set; }
    }
}
