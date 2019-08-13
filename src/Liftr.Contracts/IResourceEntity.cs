//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts
{
    public interface IResourceEntity
    {
        /// <summary>
        /// Id of the entity. This is different from the ARM resource Id.
        /// </summary>
        string EntityId { get; }

        /// <summary>
        /// Subscription Id
        /// </summary>
        string SubscriptionId { get; }

        /// <summary>
        /// Resource Group
        /// </summary>
        string ResourceGroup { get; }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of the resource.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// The location of the resource.
        /// </summary>
        string Location { get; }

        /// <summary>
        /// The tags of the resource.
        /// </summary>
        IDictionary<string, string> Tags { get; }

        ProvisioningState ProvisioningState { get; }

        DateTime CreatedUTC { get; }
    }
}
