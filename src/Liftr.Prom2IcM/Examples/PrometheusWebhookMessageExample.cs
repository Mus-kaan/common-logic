//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;

namespace Microsoft.Liftr.Prom2IcM.Examples
{
    public class PrometheusWebhookMessageExample : IExamplesProvider<PrometheusWebhookMessage>
    {
        public PrometheusWebhookMessage GetExamples()
        {
            return new PrometheusWebhookMessage()
            {
                Version = "4",
                GroupKey = "{}:{job=\"prom-rel-kube-prometheus-s-prometheus\"}",
                TruncatedAlerts = 0,
                Status = "firing",
                Receiver = "icm",
                GroupLabels = new Label()
                {
                    Job = "prom-rel-kube-prometheus-s-prometheus",
                },
                CommonLabels = new Label()
                {
                    Alertname = "PrometheusRuleFailures",
                    Instance = "10.244.1.31:9090",
                    Job = "prom-rel-kube-prometheus-s-prometheus",
                    Severity = "critical",
                },
                CommonAnnotations = new Annotation()
                {
                    summary = "[Fake Testing] This is a testing alert from local debug. Prometheus is failing rule evaluations.",
                },
                ExternalURL = "http://prom-rel-kube-prometheus-s-alertmanager.prometheus:9093",
                Alerts = new List<Alert>
                {
                    new Alert()
                    {
                        Status = "firing",
                        Labels = new Label()
                        {
                            Alertname = "[FAKE testing]KubeControllerManagerDown",
                            Severity = "critical",
                        },
                        Annotations = new Annotation()
                        {
                            description = "[FAKE testing] KubeControllerManager has disappeared from Prometheus target discovery.",
                            summary = "[FAKE testing] Target disappeared from Prometheus target discovery.",
                        },
                        StartsAt = "2020-11-22T23:55:20.148Z",
                        EndsAt = "0001-01-01T00:00:00Z",
                        GeneratorURL = "http://prom-rel-kube-prometheus-s-prometheus.prometheus:9090/graph?g0.expr=absent%28up%7Bjob%3D%22kube-controller-manager%22%7D+%3D%3D+1%29&g0.tab=1",
                    },
                    new Alert()
                    {
                        Status = "firing",
                        Labels = new Label()
                        {
                            Alertname = "[FAKE testing]page_not_found_alert",
                            Severity = "page",
                        },
                        Annotations = new Annotation()
                        {
                            description = "[FAKE testing] More than 10 404 response in the past 5 minutes",
                            summary = "[FAKE testing] Too many not found",
                        },
                        StartsAt = "2020-11-23T02:42:10.221Z",
                        EndsAt = "0001-01-01T00:00:00Z",
                        GeneratorURL = "http://prom-rel-kube-prometheus-s-prometheus.prometheus:9090/graph?g0.expr=sum%28increase%28http_requests_received_total%7Bcode%3D%22404%22%7D%5B10m%5D%29%29+%3E+10&g0.tab=1",
                    },
                },
            };
        }
    }
}
