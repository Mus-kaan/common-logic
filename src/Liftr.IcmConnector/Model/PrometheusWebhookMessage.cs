//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.IcmConnector
{
    /// <summary>
    /// Prometheus AlertManager webhook.
    /// https://godoc.org/github.com/prometheus/alertmanager/template#Data
    /// https://github.com/prometheus/alertmanager/blob/master/notify/webhook/webhook.go#L68
    /// https://github.com/prometheus/alertmanager/blob/master/template/template.go#L231
    /// https://prometheus.io/docs/alerting/latest/configuration/#webhook_config
    /// https://github.com/idealista/prom2teams/blob/develop/prom2teams/prometheus/message_schema.py#L58
    /// https://github.com/bakins/alertmanager-webhook-example/blob/master/main.go
    /// </summary>
    public class PrometheusWebhookMessage
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
        public string GeneratorURL { get; set; }

        public string Fingerprint { get; set; }
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "<Pending>")]
    public class Annotation
    {
        public string description { get; set; }

        public string summary { get; set; }

        public string message { get; set; }

        public string runbook_url { get; set; }
    }
}
