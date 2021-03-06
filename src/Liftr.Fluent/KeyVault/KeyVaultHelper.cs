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
using System.Threading;
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
            IDictionary<string, string> tags,
            CancellationToken cancellationToken)
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
                softDeleteRetentionInDays: 15,
                enableVNet: false);

            await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true, cancellationToken: cancellationToken);

            IVault vault = await liftrAzure.GetKeyVaultAsync(rgName, vaultName, cancellationToken);

            _logger.Information("Created Key Vault with resourceId {resourceId}", vault.Id);

            return vault;
        }

        public async Task<IVault> CreateKeyVaultAsync(
            ILiftrAzure liftrAzure,
            Region location,
            string rgName,
            string vaultName,
            string ipAddress,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken)
        {
            if (liftrAzure == null)
            {
                throw new ArgumentNullException(nameof(liftrAzure));
            }

            using var ops = _logger.StartTimedOperation(nameof(CreateKeyVaultAsync));
            try
            {
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
                    softDeleteRetentionInDays: 15,
                    enableVNet: true);

                await liftrAzure.CreateDeploymentAsync(location, rgName, templateContent, noLogging: true, cancellationToken: cancellationToken);

                IVault vault = await liftrAzure.GetKeyVaultAsync(rgName, vaultName, cancellationToken);

                _logger.Information("Created Key Vault with resourceId {resourceId}", vault.Id);

                return vault;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Key Vault creation failed.");
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task WithAccessFromNetworkAsync(
            IVault vault,
            ILiftrAzure liftrAzure,
            IEnumerable<string> ipList,
            IEnumerable<string> subnetList,
            CancellationToken cancellationToken,
            bool removeExistingIPs = true)
        {
            using var ops = _logger.StartTimedOperation("RestrictKeyVaultVNet");
            try
            {
                List<IPRule> ips = new List<IPRule>();
                if (!removeExistingIPs && vault.Inner.Properties?.NetworkAcls?.IpRules != null)
                {
                    foreach (var ip in vault.Inner.Properties.NetworkAcls.IpRules)
                    {
                        ips.Add(ip);
                    }
                }

                if (ipList != null)
                {
                    foreach (var ipAddress in ipList)
                    {
                        if (!ips.Where(i => i.Value.OrdinalEquals(ipAddress)).Any())
                        {
                            _logger.Information("Restricting the Key Vault '{kvId}' access to IP '{ip}'.", vault.Id, ipAddress);
                            ips.Add(new IPRule()
                            {
                                Value = ipAddress,
                            });
                        }
                    }
                }

                var subnets = new List<VirtualNetworkRule>();
                if (vault.Inner.Properties?.NetworkAcls?.VirtualNetworkRules != null)
                {
                    foreach (var role in vault.Inner.Properties.NetworkAcls.VirtualNetworkRules)
                    {
                        subnets.Add(role);
                    }
                }

                if (subnetList != null)
                {
                    foreach (var subnetId in subnetList)
                    {
                        if (!subnets.Where(sub => sub.Id.OrdinalEquals(subnetId)).Any())
                        {
                            _logger.Information("Restricting the Key Vault '{kvId}' access to subnet '{subnetId}'.", vault.Id, subnetId);
                            subnets.Add(new VirtualNetworkRule(subnetId));
                        }
                    }
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

                var softDeleteTime = await GetSoftDeleteTimeAsync(vault, liftrAzure, cancellationToken);

                var templateContent = GenerateKeyVaultTemplate(
                    vault.Region,
                    vault.Name,
                    liftrAzure.TenantId,
                    accessPolicies,
                    ips,
                    subnets,
                    tags,
                    softDeleteTime,
                    enableVNet: true);

                _logger.Information("Restricting Key Vault '{kvId}' to be accessible from IPs : '{@allowedIPs}', and subnets: '{@allowedSubnets}'", vault.Id, ips, subnets);
                await liftrAzure.CreateDeploymentAsync(vault.Region, vault.ResourceGroupName, templateContent, noLogging: true, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Key Vault VNet restriction failed.");
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task TurnOffKeyVaultNetworkRestrictionAsync(
            IVault vault,
            ILiftrAzure liftrAzure,
            CancellationToken cancellationToken)
        {
            using var ops = _logger.StartTimedOperation("TurnOffKeyVaultNetworkRestrictionAsync");
            try
            {
                List<IPRule> ips = new List<IPRule>();
                if (vault.Inner.Properties?.NetworkAcls?.IpRules != null)
                {
                    foreach (var ip in vault.Inner.Properties.NetworkAcls.IpRules)
                    {
                        ips.Add(ip);
                    }
                }

                var subnets = new List<VirtualNetworkRule>();
                if (vault.Inner.Properties?.NetworkAcls?.VirtualNetworkRules != null)
                {
                    foreach (var role in vault.Inner.Properties.NetworkAcls.VirtualNetworkRules)
                    {
                        subnets.Add(role);
                    }
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

                var softDeleteTime = await GetSoftDeleteTimeAsync(vault, liftrAzure, cancellationToken);

                var templateContent = GenerateKeyVaultTemplate(
                    vault.Region,
                    vault.Name,
                    liftrAzure.TenantId,
                    accessPolicies,
                    ips,
                    subnets,
                    tags,
                    softDeleteTime,
                    enableVNet: false);

                _logger.Information("Turning off Key Vault '{kvId}' network restrictions ...", vault.Id);
                await liftrAzure.CreateDeploymentAsync(vault.Region, vault.ResourceGroupName, templateContent, noLogging: true, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Key Vault VNet restriction failed.");
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        private static async Task<int> GetSoftDeleteTimeAsync(IVault vault, ILiftrAzure liftrAzure, CancellationToken cancellationToken)
        {
            var currentTemplate = await liftrAzure.GetResourceAsync(vault.Id, "2019-09-01", cancellationToken);
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
            int softDeleteRetentionInDays,
            bool enableVNet)
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
            else
            {
                props.networkAcls.defaultAction = enableVNet ? "Deny" : "Allow";
            }

            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }
    }
}
