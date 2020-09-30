//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Monitoring.Common
{
    public static class MonitorTagsAndDiagnosticUtils
    {
        // Rules constants.
        public const string InvalidRuleSetName = "The only name allowed for a rule set is 'default'.";
        public const int MaxNumberOfTags = 20;
        public const int MaxDiagnosticSettings = 5;
        public const string FilteringTagsLimitErrorMessage = "The maximum number of tags in each log or metric rule is 10.";
        public const string NullNamesLogRulesErrorMessage = "Tag names cannot contain only whitespaces for log rules.";

        public static string Provider { get; set; }

        public static string MonitorResourceType { get; set; } = "monitors";

        private static string DiagnosticsSettingPrefix { get; set; }

        // Utility functions.
        public static string GetMonitorsResourceGroupId(string subscriptionId, string resourceGroup)
        {
            return $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{Provider}/{MonitorResourceType}";
        }

        public static string GetMonitorsResourceId(string subscriptionId, string resourceGroup, string resourceName)
        {
            return $"{GetMonitorsResourceGroupId(subscriptionId, resourceGroup)}/{resourceName}";
        }

        public static string GetTagRulesResourceId(string resourceId, string ruleSetName)
        {
            return $"{resourceId}/tagRules/{ruleSetName}";
        }

        public static string GetTagRulesResourceId(string subscriptionId, string resourceGroup, string resourceName, string ruleSetName)
        {
            return GetTagRulesResourceId(GetMonitorsResourceId(subscriptionId, resourceGroup, resourceName), ruleSetName);
        }

        public static string GetTagRulesDefaultPath(string resourceId) => $"{resourceId}/tagRules/default";

        public static string GetDiagnosticSettingNameForResource()
        {
            return GetDiagnosticSettingNameForResource(DiagnosticsSettingPrefix);
        }

        public static string GetDiagnosticSettingNameForResource(string prefix)
        {
            return $"{prefix}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}
