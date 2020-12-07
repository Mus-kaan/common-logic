//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.DataSource.Mongo;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.MarketplaceResource.DataSource
{
    public class PaginatedResponse
    {
        public IEnumerable<MarketplaceSaasResourceEntity> Entities { get; set; }

        public DateTime? LastTimeStamp { get; set; }

        public int PageSize { get; set; }
    }
}
