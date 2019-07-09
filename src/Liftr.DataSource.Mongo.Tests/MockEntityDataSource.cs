//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public class MockEntityDataSource : ResourceEntityDataSource<MockResourceEntity>
    {
        public MockEntityDataSource(IMongoCollection<MockResourceEntity> collection)
            : base(collection)
        {
        }
    }
}
