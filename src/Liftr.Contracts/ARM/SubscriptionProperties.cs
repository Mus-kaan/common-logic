//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts.ARM
{
    public class SubscriptionProperties
    {
        public Guid? TenantId { get; set; }

        public string LocationPlacementId { get; set; }

        public string QuotaId { get; set; }
    }
}
