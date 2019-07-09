//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public class EntityId : IEntityId
    {
        public string SubscriptionId { get; set; }

        public string ResourceGroup { get; set; }

        public string ResourceName { get; set; }
    }
}
