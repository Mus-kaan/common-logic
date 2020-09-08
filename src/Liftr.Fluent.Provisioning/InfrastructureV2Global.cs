//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        public async Task<ProvisionedGlobalResources> CreateOrUpdateGlobalRGAsync(
            string baseName,
            NamingContext namingContext,
            string dnsName,
            string logAnalyticsWorkspaceId = null)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            ProvisionedGlobalResources result = new ProvisionedGlobalResources();

            try
            {
                var rgName = namingContext.ResourceGroupName(baseName);
                var kvName = namingContext.KeyVaultName(baseName);
                var acrName = namingContext.ACRName(baseName);

                var liftrAzure = _azureClientFactory.GenerateLiftrAzure();
                result.ResourceGroup = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
                if (string.IsNullOrEmpty(logAnalyticsWorkspaceId))
                {
                    var logAnalyticsName = namingContext.LogAnalyticsName(baseName);
                    logAnalyticsWorkspaceId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourcegroups/{rgName}/providers/microsoft.operationalinsights/workspaces/{logAnalyticsName}";
                    var logAnalytics = await liftrAzure.GetOrCreateLogAnalyticsWorkspaceAsync(namingContext.Location, rgName, logAnalyticsName, namingContext.Tags);
                }

                result.DnsZone = await liftrAzure.GetDNSZoneAsync(rgName, dnsName);
                if (result.DnsZone == null)
                {
                    result.DnsZone = await liftrAzure.CreateDNSZoneAsync(rgName, dnsName, namingContext.Tags);
                }

                result.KeyVault = await liftrAzure.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);

                _logger.Information("Export Key Vault '{kvId}' diagnostics to Log Analytics '{logId}'.", result.KeyVault.Id, logAnalyticsWorkspaceId);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(result.KeyVault, logAnalyticsWorkspaceId);
                await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(result.KeyVault);

                result.ContainerRegistry = await liftrAzure.GetOrCreateACRAsync(namingContext.Location, rgName, acrName, namingContext.Tags);
                _logger.Information("Export ACR '{acrId}' diagnostics to Log Analytics '{logId}'.", result.ContainerRegistry.Id, logAnalyticsWorkspaceId);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(result.ContainerRegistry, logAnalyticsWorkspaceId);

                var diagnosticsStorName = namingContext.StorageAccountName(baseName);
                var stor = await liftrAzure.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, diagnosticsStorName, namingContext.Tags);

                using (var kvValet = new KeyVaultConcierge(result.KeyVault.VaultUri, _kvClient, _logger))
                {
                    if (!await kvValet.ContainsSecretAsync(SSHUserNameSecretName))
                    {
                        _logger.Information("Storing SSH user name in global key vault.");
                        await kvValet.SetSecretAsync(SSHUserNameSecretName, "liftrvmuser", namingContext.Tags);
                    }

                    if (!await kvValet.ContainsSecretAsync(SSHPasswordSecretName))
                    {
                        _logger.Information("Storing SSH password in global key vault.");
                        await kvValet.SetSecretAsync(SSHPasswordSecretName, Guid.NewGuid().ToString(), namingContext.Tags);
                    }

                    if (File.Exists("liftr_ssh_key") && !await kvValet.ContainsSecretAsync(SSHPrivateKeySecretName))
                    {
                        _logger.Information("Storing SSH private key in global key vault.");
                        var sshPrivateKey = File.ReadAllText("liftr_ssh_key");
                        await kvValet.SetSecretAsync(SSHPrivateKeySecretName, sshPrivateKey, namingContext.Tags);
                    }

                    if (File.Exists("liftr_ssh_key.pub") && !await kvValet.ContainsSecretAsync(SSHPublicKeySecretName))
                    {
                        _logger.Information("Storing SSH public key in global key vault.");
                        var sshPublicKey = File.ReadAllText("liftr_ssh_key.pub");
                        await kvValet.SetSecretAsync(SSHPublicKeySecretName, sshPublicKey, namingContext.Tags);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(CreateOrUpdateGlobalRGAsync)} failed.");
                throw;
            }
        }
    }
}
