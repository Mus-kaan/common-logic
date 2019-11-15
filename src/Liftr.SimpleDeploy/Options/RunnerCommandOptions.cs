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
        GetComputeKeyVaultEndpoint,
        UpdateAKSPublicIpInTrafficManager,
    }

    public class RunnerCommandOptions
    {
        [Option('a', "action", Required = true, HelpText = "Action type, e.g. CreateOrUpdateGlobal, CreateOrUpdateRegionalData, CreateOrUpdateRegionalCompute, GetComputeKeyVaultEndpoint, UpdateAKSPublicIpInTrafficManager.")]
        public ActionType Action { get; set; }

        [Option('f', "file", Required = true, HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Option('s', "subscription", Required = true, HelpText = "The target subscription Id.")]
        public string SubscriptionId { get; set; }

        [Option("authFile", Required = false, HelpText = "Use auth file to login instead of managed identity.")]
        public string AuthFile { get; set; }

        [Option("dpFile", Required = false, HelpText = "File of listing all the data plane subscriptions.")]
        public string DataPlaneSubscriptionsFile { get; set; }

        [Option('l', "svcLabel", Required = false, HelpText = "The AKS service label that we will try to get the IP address of.")]
        public string AKSAppSvcLabel { get; set; }
    }
}
