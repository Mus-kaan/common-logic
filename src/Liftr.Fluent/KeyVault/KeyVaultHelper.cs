//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal class KeyVaultHelper
    {
        private const string c_keyVaultTemplateFile = "Microsoft.Liftr.Fluent.KeyVault.KeyVaultTemplate.json";
        private const string c_vnetRulesPlaceHolder = "\"PLACE_HOLDER_VNET_RULES\"";
        private const string c_ipRulesPlaceHolder = "\"PLACE_HOLDER_IP_RULES\"";
        private const string c_accessPoliciesPlaceHolder = "\"PLACE_HOLDER_ACCESS_POLICIES\"";
        private readonly Serilog.ILogger _logger;

        public KeyVaultHelper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IVault> CreateKeyVaultAsync(
            ILiftrAzure liftrAzure,
            Region location,
            string rgName,
            string vaultName,
            IDictionary<string, string> tags)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            _logger.Information("Creating a Key Vault with name '{vaultName}' in rg '{rgName}' ...", vaultName, rgName);

            var templateContent = GenerateKeyVaultTemplate(
                location,
                vaultName,
                liftrAzure.TenantId,
                null,
                null,
                null,
                tags,
                softDeleteRetentionInDays: 15);

            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true);

            IVault vault = await liftrAzure.GetKeyVaultAsync(rgName, vaultName);

            _logger.Information("Created Key Vault with resourceId {resourceId}", vault.Id);

            return vault;
        }

        public async Task<IVault> CreateKeyVaultAsync(
            ILiftrAzure liftrAzure,
            Region location,
            string rgName,
            string vaultName,
            string ipAddress,
            IDictionary<string, string> tags)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            _logger.Information("Creating a Key Vault with name '{vaultName}' only accessable from IP address '{ipAddress}' ...", vaultName, ipAddress);

            var ips = new List<IPRule>()
            {
                new IPRule()
                {
                    Value = ipAddress,
                },
            };
            var subnets = new List<VirtualNetworkRule>();

            var templateContent = GenerateKeyVaultTemplate(
                location,
                vaultName,
                liftrAzure.TenantId,
                null,
                ips,
                subnets,
                tags,
                softDeleteRetentionInDays: 15);

            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true);

            IVault vault = await liftrAzure.GetKeyVaultAsync(rgName, vaultName);

            _logger.Information("Created Key Vault with resourceId {resourceId}", vault.Id);

            return vault;
        }

        public async Task WithAccessFromNetworkAsync(
            IVault vault,
            ILiftrAzure liftrAzure,
            string ipAddress,
            string subnetId)
        {
            var ips = new List<IPRule>();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                _logger.Information("Restrict the Key Vault '{kvId}' access to IP address '{ipAddress}'.", vault.Id, ipAddress);
                ips.Add(new IPRule()
                {
                    Value = ipAddress,
                });
            }

            var subnets = new List<VirtualNetworkRule>();
            if (vault.Inner.Properties?.NetworkAcls?.VirtualNetworkRules != null)
            {
                foreach (var role in vault.Inner.Properties.NetworkAcls.VirtualNetworkRules)
                {
                    subnets.Add(role);
                }
            }

            if (!string.IsNullOrEmpty(subnetId) &&
                !subnets.Where(sub => sub.Id.OrdinalEquals(subnetId)).Any())
            {
                _logger.Information("Restrict the Key Vault '{kvId}' access to subnet '{subnetId}'.", vault.Id, subnetId);
                subnets.Add(new VirtualNetworkRule(subnetId));
            }

            var tags = new Dictionary<string, string>();
            foreach (var kvp in vault.Tags)
            {
                tags[kvp.Key] = kvp.Value;
            }

            var accessPolicies = new List<AccessPolicyEntry>();
            foreach (var policy in vault.AccessPolicies)
            {
                accessPolicies.Add(policy.Inner);
            }

            var softDeleteTime = await GetSoftDeleteTimeAsync(vault, liftrAzure);

            var templateContent = GenerateKeyVaultTemplate(
                vault.Region,
                vault.Name,
                liftrAzure.TenantId,
                accessPolicies,
                ips,
                subnets,
                tags,
                softDeleteTime);

            await liftrAzure.CreateDeploymentAsync(vault.Region, vault.ResourceGroupName, templateContent, noLogging: true);
            _logger.Information("Key Vault '{kvId}' is accessible from IPs : '{@allowedIPs}', and subnets: '{@allowedSubnets}'.", vault.Id, ips, subnets);
        }

        private static async Task<int> GetSoftDeleteTimeAsync(IVault vault, ILiftrAzure liftrAzure)
        {
            var currentTemplate = await liftrAzure.GetResourceAsync(vault.Id, "2019-09-01");
            dynamic currentObject = JObject.Parse(currentTemplate);
            return (int)currentObject.properties.softDeleteRetentionInDays;
        }

        private static string GenerateKeyVaultTemplate(
            Region location,
            string vaultName,
            string tenantId,
            IEnumerable<AccessPolicyEntry> accessPolicies,
            IEnumerable<IPRule> ips,
            IEnumerable<VirtualNetworkRule> subnets,
            IDictionary<string, string> tags,
            int softDeleteRetentionInDays)
        {
            // https://docs.microsoft.com/en-us/rest/api/keyvault/vaults/createorupdate#create-or-update-a-vault-with-network-acls
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (tags == null)
            {
                tags = new Dictionary<string, string>();
            }

            var templateContent = EmbeddedContentReader.GetContent(c_keyVaultTemplateFile);

            if (ips != null)
            {
                templateContent = templateContent.Replace(c_ipRulesPlaceHolder, ips.ToJson());
            }

            if (subnets != null)
            {
                templateContent = templateContent.Replace(c_vnetRulesPlaceHolder, subnets.ToJson());
            }

            templateContent = templateContent.Replace(c_accessPoliciesPlaceHolder, accessPolicies == null ? "[]" : accessPolicies.ToJson());

            dynamic configObj = JObject.Parse(templateContent);
            var r = configObj.resources[0];
            r.name = vaultName;
            r.location = location.ToString();
            r.tags = tags.ToJObject();

            var props = r.properties;
            props.tenantId = tenantId;
            props.softDeleteRetentionInDays = softDeleteRetentionInDays;

            if (ips == null && subnets == null)
            {
                props.networkAcls = null;
            }

            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }
    }
}
