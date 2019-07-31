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
            Location = location;

            Tags = new Dictionary<string, string>()
            {
                [nameof(PartnerName)] = PartnerName,
                [nameof(Environment)] = ShortEnvName(Environment),
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
            => $"{ShortPartnerName}-{baseName}-{ShortEnvName(Environment)}-{Location.ShortName()}-rg";

        public string KeyVaultName(string baseName)
            => $"{ShortPartnerName}-{baseName}-{Location.ShortName()}";

        public string WebAppName(string baseName)
            => $"{ShortPartnerName}-{baseName}-{ShortEnvName(Environment)}-{Location.ShortName()}";

        public string CosmosDBName(string baseName)
            => $"{ShortPartnerName}-{baseName}-{ShortEnvName(Environment)}-{Location.ShortName()}-db";

        public string VMName(string baseName, string role, int number)
            => $"{baseName}-{role}-vm{number}";

        public string VNetName(string baseName)
            => $"{baseName}-vnet";

        public string NICName(string baseName, int number)
            => $"{baseName}-nic{number}";

        public string NSGName(string baseName, string context)
            => $"{baseName}-{context}-nsg";

        public string InternalLoadBalancerName(string baseName)
            => $"{baseName}-ilb";

        public string PublicLoadBalancerName(string baseName)
            => $"{baseName}-lb";

        public string LeafDomainName(string baseName)
            => SdkContext.RandomResourceName($"{ShortPartnerName}-{baseName}-{ShortEnvName(Environment)}-{Location.ShortName()}-", 25);

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
                    return "dogfood";
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
