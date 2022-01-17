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
        string Id { get; set; } // id of the actual entity which derives from it. liftr id/partner id/mp id

        string EntityId { get; } // just a guid

        string ResourceId { get; } // arm resource id

        string AzSubsId { get; set; } // partition key

        string ResourceName { get; set; }

        bool Active { get; set; }

        DateTime CreatedUTC { get; }

        DateTime LastModifiedUTC { get; set; }

        string ETag { get; set; }
    }
}