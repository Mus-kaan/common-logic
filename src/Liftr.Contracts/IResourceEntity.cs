//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts
{
    public interface IResourceEntity
    {
        string EntityId { get; }

        string SubscriptionId { get; }

        string ResourceGroup { get; }

        string Name { get; }

        string Location { get; }

        string Tags { get; }

        string ProvisioningState { get; }

        DateTime CreatedUTC { get; }
    }
}
