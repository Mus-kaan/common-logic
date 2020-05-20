//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using System;
using System.IO;

namespace Microsoft.Liftr.Fluent
{
    public static class AzureEnvironmentExtensions
    {
        public static AzureEnvironment LoadAzEnvironment(this CloudType cloud)
        {
            switch (cloud)
            {
                case CloudType.Public:
                    return AzureEnvironment.AzureGlobalCloud;
                case CloudType.Fairfax:
                    return AzureEnvironment.AzureUSGovernment;
                case CloudType.Mooncake:
                    return AzureEnvironment.AzureChinaCloud;
                case CloudType.DogFood:
                    return new AzureEnvironment()
                    {
                        Name = nameof(CloudType.DogFood),
                        AuthenticationEndpoint = "https://login.windows-ppe.net",
                        ResourceManagerEndpoint = "https://api-dogfood.resources.windows-int.net/",
                        ManagementEndpoint = "https://management.core.cloudapi.de",
                        GraphEndpoint = "https://management.core.windows.net/",
                        StorageEndpointSuffix = "core.test-cint.azure-test.net",
                        KeyVaultSuffix = ".vault-int.azure-int.net",
                    };
                case CloudType.USNat:
                case CloudType.USSec:
                    {
                        string configFile = $"AzureEnvironment.{cloud}.json";
                        Console.WriteLine($"Load Azure Environment configurations from file: {configFile}");
                        var content = File.ReadAllText(configFile);
                        var result = content.FromJson<AzureEnvironment>();
                        return result;
                    }

                default:
                    throw new InvalidOperationException($"The cloud type '{cloud}' is not recognized.");
            }
        }
    }
}
