//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;

namespace Microsoft.Liftr.SimpleDeploy
{
    public enum ActionType
    {
        CreateOrUpdateGlobal,
        CreateOrUpdateRegionalData,
        CreateOrUpdateRegionalCompute,
        PrepareK8SAppDeployment,
        UpdateComputeIPInTrafficManager,
        OutputSubscriptionId,
        ExportACRInformation,
    }

    public class RunnerCommandOptions
    {
        [Option('a', "action", Required = true, HelpText = "Action type, e.g. CreateOrUpdateGlobal, CreateOrUpdateRegionalData, CreateOrUpdateRegionalCompute, PrepareK8SAppDeployment, UpdateComputeIPInTrafficManager, OutputSubscriptionId, ExportACRInformation.")]
        public ActionType Action { get; set; }

        [Option('f', "file", Required = false, HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; } = "hosting-options.json";

        [Option("spnObjectId", Required = false, HelpText = "The executing spn object Id.")]
        public string ExecutingSPNObjectId { get; set; }

        [Option('e', "envName", Required = true, HelpText = "Environment name.")]
        public string EnvName { get; set; }

        [Option('r', "region", Required = true, HelpText = "Azure region.")]
        public string Region { get; set; }

        [Option("authFile", Required = false, HelpText = "Use auth file to login instead of managed identity.")]
        public string AuthFile { get; set; }
    }
}
