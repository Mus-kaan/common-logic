//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.MonitoringSvc;
using System;

namespace Microsoft.Liftr.DataSource.Mongo.Tests.MonitoringSvc
{
    public class MockMonitoringRelationship : IMonitoringRelationship
    {
        public string MonitoredResourceId { get; set; }

        public string PartnerEntityId { get; set; }

        public string TenantId { get; set; }

        public string AuthorizationRuleId { get; set; }

        public string DiagnosticSettingsName { get; set; }

        public DateTime CreatedAtUTC { get; set; }

        public string EventhubName { get; set; }
    }
}
