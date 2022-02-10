//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Liftr.Monitoring.VNext.Tests
{
    public static class VNextTestConstants
    {
        public const string DiagnosticSettingsV2ApiVersion = "2017-05-01-preview";
        public const string DiagnosticSettingsProvider = "/providers/microsoft.insights/diagnosticSettings/";
        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string SubscriptionId = "/subscriptions/db854c4a-c5d8-4dad-955c-0d30d1869217";

        // Used to test Get DS operations
        public const string AcrId = "/subscriptions/db854c4a-c5d8-4dad-955c-0d30d1869217/resourceGroups/UnitTestsRG/providers/Microsoft.ContainerRegistry/registries/VNextACR01";

        public const string TenantDiagnosticSettingIDExample = "/providers/microsoft.aadiam/diagnosticSettings/vakuncha0941";

        // Used to test Create/Delete DS operations
        public const string CreateAcrId = "/subscriptions/db854c4a-c5d8-4dad-955c-0d30d1869217/resourceGroups/UnitTestsRG/providers/Microsoft.ContainerRegistry/registries/VNextACR01Create";
        public const string ResourceDiagnosticSettingsId = "/subscriptions/db854c4a-c5d8-4dad-955c-0d30d1869217/resourceGroups/UnitTestsRG/providers/Microsoft.ContainerRegistry/registries/VNextACR01/providers/microsoft.insights/diagnosticSettings/VNextDS_01";
        public const string DatadogResourceId = "/subscriptions/db854c4a-c5d8-4dad-955c-0d30d1869217/resourceGroups/UnitTestsRG/providers/Microsoft.Datadog/monitors/VNextDD01";
    }
}