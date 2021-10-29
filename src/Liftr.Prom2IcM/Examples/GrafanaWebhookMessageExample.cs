//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.IcmConnector;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;

namespace Microsoft.Liftr.Prom2IcM.Examples
{
    public class GrafanaWebhookMessageExample : IExamplesProvider<GrafanaWebhookMessage>
    {
        public GrafanaWebhookMessage GetExamples()
        {
            var evalMatches = new List<EvalMatch>();
            evalMatches.Add(new EvalMatch()
            {
                Value = 100,
                Metric = "High value",
            });

            evalMatches.Add(new EvalMatch()
            {
                Value = 200,
                Metric = "Higher value",
            });

            return new GrafanaWebhookMessage()
            {
                Title = "[Alerting] Test notification",
                RuleId = 7533874832689366674,
                RuleName = "Test notification",
                State = "alerting",
                OrgId = 0,
                DashboardId = 1,
                PanelId = 1,
                RuleUrl = "http://localhost:3000/",
                ImageUrl = "https://grafana.com/assets/img/blog/mixed_styles.png",
                Message = "Someone is testing the alert notification within Grafana.[sev3]",
                EvalMatches = evalMatches,
            };
        }
    }
}
