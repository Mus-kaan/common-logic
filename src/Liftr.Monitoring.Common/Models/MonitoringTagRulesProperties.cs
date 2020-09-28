//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.Common.Models
{
    /// <summary>
    /// Valid actions for a filtering tag. Exclusion takes priority over inclusion.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TagAction
    {
        /// <summary>
        /// Include resources with this tag.
        /// </summary>
        Include,

        /// <summary>
        /// Exclude resources with this tag.
        /// </summary>
        Exclude,
    }

    /// <summary>
    /// The definition of a filtering tag. Filtering tags are used for
    /// capturing resources and include/exclude them from being monitored.
    /// </summary>
    public class FilteringTag
    {
        /// <summary>
        /// The name (also known as the key) of the tag.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the tag.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The action for the filtering tag.
        /// </summary>
        public TagAction Action { get; set; }
    }

    /// <summary>
    /// Set of rules for sending logs for the Monitor resource.
    /// </summary>
    public class LogRules
    {
        /// <summary>
        /// Flag specifying if AAD logs should be sent for the Monitor resource.
        /// </summary>
        public bool SendAadLogs { get; set; }

        /// <summary>
        /// Flag specifying if subscription logs should be sent for the Monitor resource.
        /// </summary>
        public bool SendSubscriptionLogs { get; set; }

        /// <summary>
        /// Flag specifying if activity logs from Azure resources should be sent for the Monitor resource.
        /// </summary>
        public bool SendActivityLogs { get; set; }

        /// <summary>
        /// List of filtering tags to be used for capturing logs.
        /// This only takes effect if SendActivityLogs flag is enabled.
        /// If empty, all resources will be captured.
        /// If only Exclude action is specified, the rules will apply to the list of all available resources.
        /// If Include actions are specified, the rules will only include resources with the associated tags.
        /// </summary>
        public IEnumerable<FilteringTag> FilteringTags { get; set; }
    }

    /// <summary>
    /// Set of rules for sending metrics for the Monitor resource.
    /// </summary>
    public class MetricRules
    {
        /// <summary>
        /// List of filtering tags to be used for capturing metrics.
        /// If empty, all resources will be captured.
        /// If only Exclude action is specified, the rules will apply to the list of all available resources.
        /// If Include actions are specified, the rules will only include resources with the associated tags.
        /// </summary>
        public IEnumerable<FilteringTag> FilteringTags { get; set; }
    }

    /// <summary>
    /// Definition of the properties for a TagRules resource.
    /// </summary>
    public class MonitoringTagRulesProperties
    {
        /// <summary>
        /// Rules for sending logs to the Monitor resource.
        /// </summary>
        public LogRules LogRules { get; set; }

        /// <summary>
        /// Rules for sending metrics to the Monitor resource.
        /// </summary>
        public MetricRules MetricRules { get; set; }
    }
}
