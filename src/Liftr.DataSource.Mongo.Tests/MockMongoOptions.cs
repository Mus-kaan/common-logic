﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DataSource.Mongo.Tests
{
    public class MockMongoOptions : MongoOptions
    {
        public string SomeDummyValue { get; set; } = "dummy value";
    }
}
