//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.IcmConnector
{
    /// <summary>
    /// https://grafana.com/docs/grafana/latest/alerting/old-alerting/notifications/#webhook
    /// https://github.com/grafana/grafana/blob/main/pkg/services/alerting/notifiers/webhook.go#L111
    /// https://github.com/grafana/grafana/blob/e117b8027bb550a8ecf44ea67c84f99e2febd218/pkg/services/alerting/notifiers/webhook.go#L94
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
    public class GrafanaWebhookMessage
    {
        public string Title { get; set; }

        public long RuleId { get; set; }

        public string RuleName { get; set; }

        /// <summary>
        /// The possible values for alert state are: ok, paused, alerting, pending, no_data
        /// </summary>
        public string State { get; set; }

        public IEnumerable<EvalMatch> EvalMatches { get; set; }

        public long OrgId { get; set; }

        public long DashboardId { get; set; }

        public long PanelId { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public string RuleUrl { get; set; }

        public string ImageUrl { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// EvalMatch represents the series violating the threshold.
    /// https://github.com/grafana/grafana/blob/main/pkg/services/alerting/models.go#L40
    /// </summary>
    public class EvalMatch
    {
        public float Value { get; set; }

        public string Metric { get; set; }

        public Dictionary<string, string> Tags { get; set; }
    }
}
