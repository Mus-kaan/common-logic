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

        bool Active { get; set; }

        DateTime CreatedUTC { get; }

        DateTime LastModifiedUTC { get; set; }

        string ETag { get; set; }

        public string TenantId { get; set; }
    }
}
