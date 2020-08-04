//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.DataSource.Mongo.MonitoringSvc
{
    public class MonitoringSvcMongoOptions : MongoOptions
    {
        public string EventHubSourceEntityCollectionName { get; set; } = "metadata-evh";

        public string MonitoringRelationshipCollectionName { get; set; } = "metadata-monitoring-relationship";

        public string MonitoringStatusCollectionName { get; set; } = "metadata-monitoring-status";

        public string PartnerResourceEntityCollectionName { get; set; } = "metadata-partner";
    }
}