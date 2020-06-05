//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Liftr.Sample.Web
{
    public static class Constants
    {
        // RP constants.
        public const string DatadogResourceProvider = "Microsoft.Datadog";
        public const string MonitorsResourceType = DatadogResourceProvider + "/monitors";
        public const string TagRulesResourceType = MonitorsResourceType + "/tagRules";
        public const string ResourceRulesResourceType = MonitorsResourceType + "/resourceRules";
        public const string DatadogSsoResourceType = MonitorsResourceType + "/configureSingleSignOn";
        public const string ApiVersion2020_02_preview = "2020-02-01-preview";

        // Rules constants.
        public const string InvalidRuleSetName = "The only name allowed for a rule set is 'default'.";
        public const int MaxNumberOfTags = 10;
        public const string FilteringTagsLimitErrorMessage = "The maximum number of tags in each log or metric rule is 10.";

        // Utility functions.
        public static string GetMonitorsResourceId(string subscriptionId, string resourceGroup, string resourceName)
        {
            return $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{MonitorsResourceType}/{resourceName}";
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

        public static string GetConfigureDatadogSsoResourceId(string resourceId)
        {
            return $"{resourceId}/configureSingleSignOn";
        }

        public static string GetDatadogSsoId(string resourceId)
        {
            return $"{resourceId}/getSingleSignOn";
        }

        public static string GetDiagnosticSettingNameForResource(string monitorName)
        {
            return $"DATADOG_DS_" + monitorName;
        }
    }
}
