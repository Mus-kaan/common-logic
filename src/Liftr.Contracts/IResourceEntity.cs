//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Contracts
{
    public interface IResourceEntity
    {
        /// <summary>
        /// Id of the entity. This is different from the ARM resource Id.
        /// </summary>
        string EntityId { get; }

        /// <summary>
        /// ARM resource Id.
        /// </summary>
        string ResourceId { get; }

        ProvisioningState ProvisioningState { get; }

        DateTime CreatedUTC { get; }

        string ETag { get; set; }
    }
}
