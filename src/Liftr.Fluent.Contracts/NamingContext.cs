//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Liftr.Fluent.Contracts
{
    /// <summary>
    /// This class implemented some recommended naming conventions.
    /// https://docs.microsoft.com/en-us/azure/architecture/best-practices/naming-conventions
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-using-tags
    /// https://docs.microsoft.com/en-us/azure/architecture/cloud-adoption/decision-guides/resource-tagging/
    /// https://aws.amazon.com/answers/account-management/aws-tagging-strategies/
    /// </summary>
    public class NamingContext
    {
        public const string c_RegionTagName = "RegionTag";
        public const string c_createdAtTagName = "FirstCreatedAt";
        public const string c_versionTagName = "LiftrFluentLibraryVersion";

        public NamingContext(string partnerName, string shortPartnerName, EnvironmentType environment, Region location)
        {
            PartnerName = partnerName ?? throw new ArgumentNullException(nameof(partnerName));
            ShortPartnerName = shortPartnerName?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(shortPartnerName));
            Environment = environment;
            Location = location ?? throw new ArgumentNullException(nameof(location));

            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            Tags = new Dictionary<string, string>()
            {
                [nameof(PartnerName)] = PartnerName,
                [nameof(Environment)] = Environment.ToString(),
                [c_RegionTagName] = location.ToString(),
                [c_createdAtTagName] = DateTime.UtcNow.ToZuluString(),
                [c_versionTagName] = version,
            };
        }

        public string PartnerName { get; }

        public string ShortPartnerName { get; }

        public EnvironmentType Environment { get; }

        public string ShortEnvironmentName => Environment.ShortName();

        public Region Location { get; }

        public IDictionary<string, string> Tags { get; }

        public void SetRoleTag(string roleName)
        {
            Tags["role"] = roleName;
        }

        public string ResourceGroupName(string baseName)
            => GenerateCommonName(baseName, "rg");

        public string AKSManagedRGName(string baseName)
        {
            // https://docs.microsoft.com/en-us/azure/aks/faq#can-i-provide-my-own-name-for-the-aks-node-resource-group
            // MC_resourcegroupname_clustername_location
            return $"MC_{ResourceGroupName(baseName)}_{AKSName(baseName)}_{Location.Name}";
        }

        public string StorageAccountName(string baseName)
        {
            var name = "st" + GenerateCommonName(baseName, suffix: null, noRegion: false, delimiter: string.Empty);
            name = name.ToLowerInvariant();
            if (name.Length > 24)
            {
                throw new InvalidOperationException($"{nameof(StorageAccountName)} cannot be longer than 24 characters.");
            }

            return name;
        }

        public string NetworkName(string baseName)
            => GenerateCommonName(baseName, "vnet");

        public string SubnetName(string baseName)
            => GenerateCommonName(baseName, delimiter: string.Empty);

        public string MSIName(string baseName)
            => GenerateCommonName(baseName, "msi");

        public string KeyVaultName(string baseName)
            => GenerateCommonName(baseName, "kv", delimiter: string.Empty);

        public string WebAppName(string baseName)
            => GenerateCommonName(baseName);

        public string AKSName(string baseName)
            => GenerateCommonName(baseName, "aks");

        public string TrafficManagerName(string baseName)
            => GenerateCommonName(baseName, "tm");

        public string CosmosDBName(string baseName)
            => GenerateCommonName(baseName, "db");

        public string LogAnalyticsName(string baseName)
            => GenerateCommonName(baseName, "log");

        public string SharedImageGalleryName(string baseName)
           => GenerateCommonName(baseName, "sig", noRegion: false, delimiter: "_");

        public string ACRName(string baseName)
            => GenerateCommonName(baseName, "acr", delimiter: string.Empty);

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
            => SdkContext.RandomResourceName($"{ShortPartnerName}-{baseName}-{Environment.ShortName()}-{Location.ShortName()}-", 25);

        public string GenerateCommonName(string baseName, string suffix = null, bool noRegion = false, string delimiter = "-")
        {
            var name = $"{ShortPartnerName}{delimiter}{Environment.ShortName()}{delimiter}{baseName}";

            if (!noRegion)
            {
                name = $"{name}{delimiter}{Location.ShortName()}";
            }

            if (!string.IsNullOrEmpty(suffix))
            {
                name = $"{name}{delimiter}{suffix}";
            }

            return name;
        }
    }
}
