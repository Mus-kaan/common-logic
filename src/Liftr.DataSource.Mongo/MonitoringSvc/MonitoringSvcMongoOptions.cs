//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcMongoOptions : MongoOptions
    {
        public string VmExtensionDetailsEntityCollectionName { get; set; }

        public string EventHubSourceEntityCollectionName { get; set; }

        public string MonitoredEntityCollectionName { get; set; }
    }
}
