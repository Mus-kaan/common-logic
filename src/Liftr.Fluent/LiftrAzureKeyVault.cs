//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    internal partial class LiftrAzure
    {
        #region Key Vault
        public async Task<IVault> GetOrCreateKeyVaultAsync(
            Region location,
            string rgName,
            string vaultName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var kv = await GetKeyVaultAsync(rgName, vaultName, cancellationToken);

            if (kv == null)
            {
                kv = await CreateKeyVaultAsync(location, rgName, vaultName, tags, cancellationToken);
            }

            return kv;
        }

        public async Task<IVault> GetOrCreateKeyVaultAsync(
            Region location,
            string rgName,
            string vaultName,
            string accessibleFromIP,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var kv = await GetKeyVaultAsync(rgName, vaultName, cancellationToken);

            if (kv == null)
            {
                var helper = new KeyVaultHelper(_logger);
                kv = await helper.CreateKeyVaultAsync(this, location, rgName, vaultName, accessibleFromIP, tags, cancellationToken);
            }

            return kv;
        }

        public Task WithKeyVaultAccessFromNetworkAsync(
            IVault vault,
            string ipAddress,
            string subnetId,
            bool enableVNetFilter = true,
            bool removeExistingIPs = true,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> ipList = string.IsNullOrEmpty(ipAddress) ? null : new List<string> { ipAddress };
            IEnumerable<string> subnetList = string.IsNullOrEmpty(subnetId) ? null : new List<string> { subnetId };
            return WithKeyVaultAccessFromNetworkAsync(vault, ipList, subnetList, enableVNetFilter, removeExistingIPs, cancellationToken);
        }

        public async Task WithKeyVaultAccessFromNetworkAsync(
            IVault vault,
            IEnumerable<string> ipList,
            IEnumerable<string> subnetList,
            bool enableVNetFilter = true,
            bool removeExistingIPs = true,
            CancellationToken cancellationToken = default)
        {
            if (vault == null)
            {
                throw new ArgumentNullException(nameof(vault));
            }

            if (ipList == null && subnetList == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException($"{nameof(ipList)} and {nameof(subnetList)} cannot be both null");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            if (!enableVNetFilter && vault?.Inner?.Properties?.NetworkAcls?.DefaultAction != NetworkRuleAction.Deny)
            {
                _logger.Information("Skip adding VNet since Network isolation is not enabled for key vault {kvId}", vault.Id);
                return;
            }

            var helper = new KeyVaultHelper(_logger);
            await helper.WithAccessFromNetworkAsync(vault, this, ipList, subnetList, cancellationToken, removeExistingIPs);
        }

        public async Task<IVault> CreateKeyVaultAsync(
            Region location,
            string rgName,
            string vaultName,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken = default)
        {
            var helper = new KeyVaultHelper(_logger);
            var kv = await helper.CreateKeyVaultAsync(this, location, rgName, vaultName, tags, cancellationToken);
            return kv;
        }

        public async Task<IVault> GetKeyVaultAsync(string rgName, string vaultName, CancellationToken cancellationToken = default)
        {
            _logger.Information("Getting Key Vault. rgName: {rgName}, vaultName: {vaultName} ...", rgName, vaultName);
            var stor = await FluentClient
                .Vaults
                .GetByResourceGroupAsync(rgName, vaultName, cancellationToken);

            if (stor == null)
            {
                _logger.Information("Cannot find Key Vault. rgName: {rgName}, vaultName: {vaultName} ...", rgName, vaultName);
            }

            return stor;
        }

        public async Task<IVault> GetKeyVaultByIdAsync(string kvResourceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Information($"Getting KeyVault with resource Id {kvResourceId} ...");
                return await FluentClient.Vaults.GetByIdAsync(kvResourceId, cancellationToken);
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                return null;
            }
        }

        public async Task<IEnumerable<IVault>> ListKeyVaultAsync(string rgName, string namePrefix = null, CancellationToken cancellationToken = default)
        {
            _logger.Information($"Listing KeyVault in resource group {rgName} with prefix {namePrefix} ...");
            IEnumerable<IVault> kvs = (await FluentClient
                .Vaults
                .ListByResourceGroupAsync(rgName, loadAllPages: true, cancellationToken: cancellationToken)).ToList();

            if (!string.IsNullOrEmpty(namePrefix))
            {
                kvs = kvs.Where((kv) => kv.Name.OrdinalStartsWith(namePrefix));
            }

            return kvs;
        }

        public async Task RemoveAccessPolicyAsync(string kvResourceId, string servicePrincipalObjectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(servicePrincipalObjectId))
            {
                throw new ArgumentNullException(nameof(servicePrincipalObjectId));
            }

            var vault = await GetKeyVaultByIdAsync(kvResourceId, cancellationToken) ?? throw new InvalidOperationException("Cannt find vault with resource Id: " + kvResourceId);
            await vault.Update().WithoutAccessPolicy(servicePrincipalObjectId).ApplyAsync(cancellationToken);
            _logger.Information("Finished removing KeyVault '{kvResourceId}' access policy of '{servicePrincipalObjectId}'", kvResourceId, servicePrincipalObjectId);
        }

        public async Task GrantSelfKeyVaultAdminAccessAsync(IVault kv, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(SPNObjectId))
            {
                var ex = new ArgumentException($"{nameof(SPNObjectId)} is empty. Cannot grant access.");
                _logger.Error(ex, $"Please set {nameof(SPNObjectId)} in the constructor.");
                throw ex;
            }

            await kv.Update()
                .DefineAccessPolicy()
                .ForObjectId(SPNObjectId)
                .AllowKeyAllPermissions()
                .AllowSecretAllPermissions()
                .AllowCertificateAllPermissions()
                .Attach()
                .ApplyAsync(cancellationToken);

            _logger.Information("Granted all secret and certificate permissions (access policy) of key vault '{kvId}' to the excuting SPN with object Id '{SPNObjectId}'.", kv.Id, SPNObjectId);
        }

        public async Task RemoveSelfKeyVaultAccessAsync(IVault kv, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(SPNObjectId))
            {
                var ex = new ArgumentException($"{nameof(SPNObjectId)} is empty. Cannot grant access.");
                _logger.Error(ex, $"Please set {nameof(SPNObjectId)} in the constructor.");
                throw ex;
            }

            await kv.Update().WithoutAccessPolicy(SPNObjectId).ApplyAsync(cancellationToken);

            _logger.Information("Removed access of the excuting SPN with object Id '{SPNObjectId}' to key vault '{kvId}'", SPNObjectId, kv.Id);
        }

        #endregion Key Vault
    }
}
