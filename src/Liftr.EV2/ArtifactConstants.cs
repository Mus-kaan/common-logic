//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2
{
    public static class ArtifactConstants
    {
        public const string c_AppImgFolderName = "0_app_img";

        public const string c_GlobalFolderName = "1_global";

        public const string c_RegionalDataFolderName = "2_regional_data";

        public const string c_RegionalComputeFolderName = "3_regional_compute";

        public const string c_ApplicationFolderName = "4_deploy_aks_app";

        public const string c_TMFolderName = "5_traffic_manager";

        public const string c_ImageBuilderFolderName = "image_builder";

        public const string c_EV2ShellExtensionType = "LiftrCustomShellExtension";

        public const string c_EV2ServiceResourceGroupDefinitionName = "liftr-ev2-shell-deployment";

        public const string c_LiftrCommonImageVersions = "LiftrCommonImageVersions";

        public const string c_ScopeBindingsFileName = "ScopeBindings.json";

        public static string ServiceModelFileName(EnvironmentType environment)
        {
            return $"ServiceModel.{environment}.json";
        }

        public static string ServiceModelFileName(string environment)
        {
            return $"ServiceModel.{environment}.json";
        }

        public static string RolloutSpecFileName(EnvironmentType environment, string region)
        {
            return $"RolloutSpec.{environment}.{region}.json";
        }

        public static string RolloutSpecFileName(string baseName)
        {
            return $"RolloutSpec.{baseName}.json";
        }

        public static string RolloutParametersPath(string environment, string region)
        {
            return $"parameters\\{environment}\\{RolloutParametersFileName(environment, region)}";
        }

        public static string RolloutParametersFileName(string environment, string region)
        {
            return $"RolloutParameters.{environment}.{region}.json";
        }

        public static string RolloutParametersFileName(string baseName)
        {
            return $"RolloutParameters.{baseName}.json";
        }
    }
}
