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
        GetKeyVaultEndpoint,
        UpdateAKSPublicIpInTrafficManager,
    }

    public class RunnerCommandOptions
    {
        [Option('a', "action", Required = true, HelpText = "Action type, e.g. CreateOrUpdateGlobal, CreateOrUpdateRegionalData, CreateOrUpdateRegionalCompute, GetKeyVaultEndpoint, UpdateAKSPublicIpInTrafficManager.")]
        public ActionType Action { get; set; }

        [Option('f', "file", Required = true, HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Option('s', "subscription", Required = true, HelpText = "The target subscription Id.")]
        public string SubscriptionId { get; set; }

        [Option("authFile", Required = false, HelpText = "Use auth file to login instead of managed identity.")]
        public string AuthFile { get; set; }

        [Option("activeKey", Required = false, HelpText = "Name of the active cosmos DB connection String.")]
        public string ActiveKeyName { get; set; }

        [Option('l', "svcLabel", Required = false, HelpText = "The AKS service label that we will try to get the IP address of.")]
        public string AKSAppSvcLabel { get; set; }
    }
}
