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

        bool Active { get; set; }

        DateTime CreatedUtc { get; }

        DateTime LastModifiedUtc { get; set; }

        string ETag { get; set; }

        bool IsDeleted { get; set; }
    }
}
