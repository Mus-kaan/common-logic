//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DataSource.Mongo
{
    public class MongoOptions
    {
        public string ConnectionString { get; set; }

        public string DatabaseName { get; set; }

        public bool LogDBOperation { get; set; } = true;
    }
}
