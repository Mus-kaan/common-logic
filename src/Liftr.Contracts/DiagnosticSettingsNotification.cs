//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Contracts
{
    public class DiagnosticSettingsNotification
    {
        public DiagnosticSettingsNotification(string eventType, string monitorId, string diagnosticSettingsId, string tenantId)
        {
            MonitorId = monitorId;
            EventType = eventType;
            DiagnosticSettingsId = diagnosticSettingsId;
            TenantId = tenantId;
        }

        public string MonitorId { get; set; }

        public string EventType { get; set; }

        public string DiagnosticSettingsId { get; set; }

        public string TenantId { get; set; }
    }
}
