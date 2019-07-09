//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class ResourceEntityDataSource
    {
        private readonly IMongoCollection<IResourceEntity> _collection;
        private readonly ITimeSource _timeSource;

        public ResourceEntityDataSource(IMongoCollection<IResourceEntity> collection, ITimeSource timeSource)
        {
            _collection = collection;
            _timeSource = timeSource;
        }
    }
}
