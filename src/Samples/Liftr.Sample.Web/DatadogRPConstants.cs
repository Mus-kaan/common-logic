//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Liftr.Sample.Web
{
    public static class DatadogRPConstants
    {
        /// <summary>
        /// /providers/Microsoft.Datadog/operations
        /// </summary>
        public const string DatadogOperationsRoute = "/providers/" + Constants.DatadogResourceProvider + "/operations";

        /// <summary>
        /// /subscriptions/{subscriptionId}
        /// </summary>
        public const string SubscriptionRoute = "/subscriptions/{subscriptionId}";

        /// <summary>
        /// subscriptions/{subscriptionId}/providers/Microsoft.Datadog/monitors
        /// </summary>
        public const string MonitorsRoute = SubscriptionRoute + "/providers/" + Constants.MonitorsResourceType;

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors
        /// </summary>
        public const string MonitorsByResourceGroupRoute = SubscriptionRoute + "/resourceGroups/{resourceGroupName}/providers/" + Constants.MonitorsResourceType;

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}
        /// </summary>
        public const string MonitorRoute = MonitorsByResourceGroupRoute + "/{monitorName}";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/ResourceCreationValidate
        /// </summary>
        public const string MonitorCreationValidateRoute = MonitorRoute + "/ResourceCreationValidate";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/tagRules
        /// </summary>
        public const string TagRulesRoute = MonitorRoute + "/tagRules";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/tagRules/{ruleSetName}
        /// </summary>
        public const string TagRulesDefaultRoute = TagRulesRoute + "/{ruleSetName}";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/tagRules/{ruleSetName}/ResourceCreationValidate
        /// </summary>
        public const string TagRulesCreationValidateRoute = TagRulesDefaultRoute + "/ResourceCreationValidate";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/listApiKeys
        /// </summary>
        public const string ApiKeysRoute = MonitorRoute + "/listApiKeys";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/getDefaultKey
        /// </summary>
        public const string GetDefaultKeyRoute = MonitorRoute + "/getDefaultKey";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/setDefaultKey
        /// </summary>
        public const string SetDefaultKeyRoute = MonitorRoute + "/setDefaultKey";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/configureSignSignOn
        /// </summary>
        public const string ConfigureMonitorSingleSignOn = MonitorRoute + "/configureSingleSignOn";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/getSingleSignOn
        /// </summary>
        public const string GetMonitorSingleSignOn = MonitorRoute + "/getSingleSignOn";

        /// <summary>
        /// subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Datadog/monitors/{monitorName}/listMonitoredResources
        /// </summary>
        public const string ListMonitoredResourcesRoute = MonitorRoute + "/listMonitoredResources";
    }
}
