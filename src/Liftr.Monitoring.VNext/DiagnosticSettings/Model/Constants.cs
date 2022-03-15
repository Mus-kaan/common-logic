//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model
{
    public static class Constants
    {
        public const string ArmManagementEndpoint = "https://management.azure.com";

        public const string AzureResourceType = "Microsoft.Insights/diagnosticSettings";

        public const string ArmDiagnosticSettingsCategoryList = "/providers/Microsoft.Insights/diagnosticSettingsCategories";

        public const string LogCategoriesCachKeyPrefix = "LogCagtegories_CacheKey_";

        public static List<string> SubscriptionLogCategories = new List<string> { "Administrative", "Security", "ServiceHealth", "Alert", "Recommendation", "Policy", "Autoscale", "ResourceHealth" };

        public const string DiagnosticSettingsV2ApiVersion = "2021-05-01-preview";
    }
}
