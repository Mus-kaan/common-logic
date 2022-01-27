//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.DBService.Contracts.Interfaces
{
    public interface IEntity
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; } // mongo id of the liftr/partner/mp resource

        string ResourceId { get; } // arm resource id

        string AzSubsId { get; set; } // partition key

        string TenantId { get; set; } // tenat id

        string ResourceName { get; set; }

        bool Active { get; set; }

        DateTime CreatedUTC { get; }

        DateTime LastModifiedUTC { get; set; }

        string ETag { get; set; }

        bool IsDeleted { get; set; }
    }
}