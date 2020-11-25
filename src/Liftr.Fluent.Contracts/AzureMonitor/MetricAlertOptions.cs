//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Fluent.Contracts.AzureMonitor
{
    public class MetricAlertOptions
    {
        public string Name { get; set; }

        public int Severity { get; set; }

        public string Description { get; set; }

        public string TargetResourceId { get; set; }

        public string ActionGroupResourceId { get; set; }

        public string AlertConditionName { get; set; }

        public int AggregationPeriod { get; set; }

        public int FrequencyOfEvaluation { get; set; }

        public string MetricName { get; set; }

        public string MetricNamespace { get; set; }

        public string TimeAggregationType { get; set; }

        public string ConditionOperator { get; set; }

        public double Threshold { get; set; }
    }
}
