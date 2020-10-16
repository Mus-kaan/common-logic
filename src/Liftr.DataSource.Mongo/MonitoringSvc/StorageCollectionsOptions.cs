//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class StorageCollectionsOptions : MongoOptions
    {
        public string StorageEntityCollectionName { get; set; } = "metadata-stor";
    }
}
