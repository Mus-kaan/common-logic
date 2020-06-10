//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DataSource.MonitoringSvc;
using MongoDB.Driver;

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class PartnerResourceDataSource : ResourceEntityDataSource<PartnerResourceEntity>, IPartnerResourceDataSource<PartnerResourceEntity>
    {
        public PartnerResourceDataSource(IMongoCollection<PartnerResourceEntity> collection, ITimeSource timeSource)
            : base(collection, timeSource)
        {
        }
    }
}
