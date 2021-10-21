//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Prom2IcM
{
    /// <summary>
    /// https://godoc.org/github.com/prometheus/alertmanager/template#Data
    /// https://github.com/prometheus/alertmanager/blob/master/notify/webhook/webhook.go#L68
    /// https://github.com/prometheus/alertmanager/blob/master/template/template.go#L231
    /// https://prometheus.io/docs/alerting/latest/configuration/#webhook_config
    /// https://github.com/idealista/prom2teams/blob/develop/prom2teams/prometheus/message_schema.py#L58
    /// https://github.com/bakins/alertmanager-webhook-example/blob/master/main.go
    /// </summary>
    public class WebhookMessage
    {
        public string Version { get; set; }

        /// <summary>
        /// key identifying the group of alerts (e.g. to deduplicate)
        /// </summary>
        public string GroupKey { get; set; }

        /// <summary>
        /// how many alerts have been truncated due to "max_alerts"
        /// </summary>
        public int TruncatedAlerts { get; set; }

        /// <summary>
        /// resolved | firing
        /// </summary>
        public string Status { get; set; } = "unknown";

        public string Receiver { get; set; }

        public Label GroupLabels { get; set; }

        public Label CommonLabels { get; set; }

        public Annotation CommonAnnotations { get; set; }

        /// <summary>
        /// backlink to the Alertmanager.
        /// </summary>
        public string ExternalURL { get; set; }

        public IEnumerable<Alert> Alerts { get; set; }
    }

    public class Alert
    {
        /// <summary>
        /// resolved | firing
        /// </summary>
        public string Status { get; set; }

        public Label Labels { get; set; }

        public Annotation Annotations { get; set; }

        public string StartsAt { get; set; }

        public string EndsAt { get; set; }

        /// <summary>
        /// identifies the entity that caused the alert
        /// </summary>
        public string GeneratorURL { get; set; }
    }

    public class Label
    {
        public string Alertname { get; set; }

        public string Device { get; set; }

        public string Fstype { get; set; }

        public string Instance { get; set; }

        public string Job { get; set; }

        public string Mountpoint { get; set; }

        public string Severity { get; set; }
    }

    public class Annotation
    {
        public string Description { get; set; }

        public string Summary { get; set; }
    }
}
