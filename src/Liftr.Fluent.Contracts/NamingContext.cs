//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

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

        public string ResourceGroupName(string name)
            => $"{ShortPartnerName}-{name}-{ShortEnvName(Environment)}-{Location.ShortName()}-rg";

        public string KeyVaultName(string name)
            => $"{ShortPartnerName}-{name}-{Location.ShortName()}";

        public string WebAppName(string name)
            => $"{ShortPartnerName}-{name}-{ShortEnvName(Environment)}-{Location.ShortName()}";

        public string CosmosDBName(string name)
            => $"{ShortPartnerName}-{name}-{ShortEnvName(Environment)}-{Location.ShortName()}-db";

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
