//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;

namespace Microsoft.Liftr.Monitoring.Common.Models
{
    /// <summary>
    /// Capture logs and metrics of Azure resources based on ARM tags.
    /// </summary>
    public class MonitoringTagRules
    {
        /// <summary>
        /// Name of the rule set.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public string Name { get; set; }

        /// <summary>
        /// The id of the rule set.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public string Id { get; set; }

        /// <summary>
        /// The type of the rule set.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public string Type { get; set; }

        /// <summary>
        /// The properties of the tag rule set.
        /// </summary>
        public MonitoringTagRulesProperties Properties { get; set; }
    }
}
