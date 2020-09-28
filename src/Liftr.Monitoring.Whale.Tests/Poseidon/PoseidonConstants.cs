//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Monitoring.Common.Models;

namespace Microsoft.Liftr.Monitoring.Whale.Poseidon.Tests
{
    public static class PoseidonConstants
    {
        // Generic constants.
        public const string TestResourceGroup = "whale-poseidon-rg";
        public const string TestLocationName = "westus2";
        public const string TestInvalidLocationName = "eastus";
        public const string ApiVersion2020_02_preview = "2020-02-01-preview";

        // Resource constants.
        public const string EventHubNamespaceName = "whale-poseidon";
        public const string EventHubAuthorizationRuleName = "RootManageSharedAccessKey";
        public const string EventHubName = "poseidon";
        public const string PublicIp1Name = "test-ip-1";
        public const string PublicIp2Name = "test-ip-2";
        public const string DatadogMonitorName1 = "flumine"; // Latin for river
        public const string DatadogMonitorName2 = "elv"; // Norwegian for river
        public const string DatadogEntityId1 = "000000000000000000000000";
        public const string DatadogEntityId2 = "111111111111111111111111";

        public static string PublicIp1Id
        {
            get
            {
                return $"/subscriptions/{TestCredentials.SubscriptionId}/resourceGroups/{TestResourceGroup}/providers/Microsoft.Network/publicIPAddresses/{PublicIp1Name}";
            }
        }

        public static string PublicIp2Id
        {
            get
            {
                return $"/subscriptions/{TestCredentials.SubscriptionId}/resourceGroups/{TestResourceGroup}/providers/Microsoft.Network/publicIPAddresses/{PublicIp2Name}";
            }
        }

        public static string DatadogMonitorId1
        {
            get
            {
                return $"/subscriptions/{TestCredentials.SubscriptionId}/resourceGroups/{TestResourceGroup}/providers/Microsoft.Datadog/monitors/{DatadogMonitorName1}";
            }
        }

        public static string DatadogMonitorId2
        {
            get
            {
                return $"/subscriptions/{TestCredentials.SubscriptionId}/resourceGroups/{TestResourceGroup}/providers/Microsoft.Datadog/monitors/{DatadogMonitorName2}";
            }
        }

        public static string SubscriptionResourceId
        {
            get
            {
                return $"/subscriptions/{TestCredentials.SubscriptionId}";
            }
        }

        // Filtering tag constants.
        public static FilteringTag InclusionFilteringTag
        {
            get
            {
                return new FilteringTag()
                {
                    Name = "Environment",
                    Value = "Prod",
                    Action = TagAction.Include,
                };
            }
        }

        public static FilteringTag ExclusionFilteringTag
        {
            get
            {
                return new FilteringTag()
                {
                    Name = "Datadog",
                    Value = "False",
                    Action = TagAction.Exclude,
                };
            }
        }

        public static string GetNumberedEventHubNamespace(int idx)
        {
            return $"{EventHubNamespaceName}{idx}";
        }

        public static string GetNumberedDiagnosticSettingName(int idx)
        {
            return $"nehir_{idx}"; // Turkish for river
        }
    }
}
