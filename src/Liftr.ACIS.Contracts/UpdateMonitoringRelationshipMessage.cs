//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.ACIS.Contracts
{
    public class UpdateMonitoringRelationshipMessage
    {
        /// <summary>
        /// The resource Id of the Datadog monitor resource.
        /// </summary>
        public string MonitorResourceId { get; set; }

        /// <summary>
        /// The resource Id of the resource being monitored.
        /// </summary>
        public string MonitoringResourceId { get; set; }

        public string DiagnosticSettingsName { get; set; }

        public string EventhubName { get; set; }

        public string AuthorizationRuleId { get; set; }
    }
}
