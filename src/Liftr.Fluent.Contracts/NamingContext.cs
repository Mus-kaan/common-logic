//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public enum EnvironmentType
    {
        Production,
        PPE,
        DogFood,
        Dev,
        Test,
    }

    /// <summary>
    /// This class implemented some recommended naming conventions.
    /// https://docs.microsoft.com/en-us/azure/architecture/best-practices/naming-conventions
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-using-tags
    /// https://docs.microsoft.com/en-us/azure/architecture/cloud-adoption/decision-guides/resource-tagging/
    /// https://aws.amazon.com/answers/account-management/aws-tagging-strategies/
    /// </summary>
    public class NamingContext
    {
        public const string c_infraVersionTagName = "InfraVersion";
        public const string c_RegionTagName = "RegionTag"; // TODO: this might be unuseful?

        public NamingContext(string partnerName, string shortPartnerName, EnvironmentType environment, Region location)
        {
            PartnerName = partnerName ?? throw new ArgumentNullException(nameof(partnerName));
            ShortPartnerName = shortPartnerName?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(shortPartnerName));
            Environment = environment;
            Location = location ?? throw new ArgumentNullException(nameof(location));

            Tags = new Dictionary<string, string>()
            {
                [nameof(PartnerName)] = PartnerName,
                [nameof(Environment)] = Environment.ToString(),
                [c_infraVersionTagName] = "v1",
                [c_RegionTagName] = location.ToString(),
            };
        }

        public string PartnerName { get; }

        public string ShortPartnerName { get; }

        public EnvironmentType Environment { get; }

        public Region Location { get; }

        public IDictionary<string, string> Tags { get; }

        public void SetRoleTag(string roleName)
        {
            Tags["role"] = roleName;
        }

        public string ResourceGroupName(string baseName)
            => GenerateCommonName(baseName, "rg");

        public string MSIName(string baseName)
            => GenerateCommonName(baseName, "msi");

        public string KeyVaultName(string baseName)
            => GenerateCommonName(baseName, "kv");

        public string WebAppName(string baseName)
            => GenerateCommonName(baseName);

        public string AKSName(string baseName)
            => GenerateCommonName(baseName, "aks");

        public string TrafficManagerName(string baseName)
            => GenerateCommonName(baseName, "tm");

        public string CosmosDBName(string baseName)
            => GenerateCommonName(baseName, "db");

        public static string DiskName(string baseName, int number)
            => $"{baseName}-disk{number}";

        public static string DiskName(string diskType, string identifier)
            => $"{diskType}-disk{identifier}";

        public static string VMName(string baseName, string role, int number)
            => $"{baseName}-{role}-vm{number}";

        public static string VMName(string baseName, string role, string identifier)
            => $"{baseName}-{role}-vm{identifier}";

        public static string IdentifierFromVMName(string baseName, string role, string vmName)
            => vmName?.Substring($"{baseName}-{role}-vm".Length) ?? throw new ArgumentNullException(nameof(vmName));

        public static string VNetName(string baseName)
            => $"{baseName}-vnet";

        public static string NICName(string baseName, int number)
            => $"{baseName}-nic{number}";

        public static string NICName(string baseName, string identifier)
            => $"{baseName}-nic{identifier}";

        public static string NSGName(string baseName, string context)
            => $"{baseName}-{context}-nsg";

        public static string InternalLoadBalancerName(string baseName)
            => $"{baseName}-ilb";

        public static string PublicLoadBalancerName(string baseName)
            => $"{baseName}-lb";

        public static string PublicIPName(string vmOrServiceName)
            => $"{vmOrServiceName}-pip";

        public string LeafDomainName(string baseName)
            => SdkContext.RandomResourceName($"{ShortPartnerName}-{baseName}-{ShortEnvName(Environment)}-{Location.ShortName()}-", 25);

        public string GenerateCommonName(string baseName, string suffix = null, bool noRegion = false)
        {
            var name = $"{ShortPartnerName}-{ShortEnvName(Environment)}-{baseName}";

            if (!noRegion)
            {
                name = $"{name}-{Location.ShortName()}";
            }

            if (!string.IsNullOrEmpty(suffix))
            {
                name = $"{name}-{suffix}";
            }

            return name;
        }

        #region Private
        private static string ShortEnvName(EnvironmentType env)
        {
            switch (env)
            {
                case EnvironmentType.Production:
                    return "prod";
                case EnvironmentType.PPE:
                    return "ppe";
                case EnvironmentType.DogFood:
                    return "df";
                case EnvironmentType.Dev:
                    return "dev";
                case EnvironmentType.Test:
                    return "test";
                default:
                    throw new ArgumentOutOfRangeException(nameof(env));
            }
        }
        #endregion
    }
}
