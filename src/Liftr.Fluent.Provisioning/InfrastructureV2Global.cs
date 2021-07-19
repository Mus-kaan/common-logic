//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.TrafficManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        public async Task<ProvisionedGlobalResources> CreateOrUpdateGlobalRGAsync(
            string baseName,
            NamingContext namingContext,
            string dnsName,
            bool addGlobalDB,
            string secretPrefix = null,
            PartnerCredentialUpdateOptions partnerCredentialUpdateConfig = null,
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
                var trafficManagerName = namingContext.TrafficManagerName(baseName);

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

                result.GlobalTrafficManager = await liftrAzure.GetOrCreateTrafficManagerAsync(rgName, trafficManagerName, namingContext.Tags);
                await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(result.GlobalTrafficManager, logAnalyticsWorkspaceId);

                if (result.GlobalTrafficManager.TrafficRoutingMethod != TrafficRoutingMethod.Performance)
                {
                    result.GlobalTrafficManager = await result.GlobalTrafficManager.Update().WithPerformanceBasedRouting().ApplyAsync();
                }

                _logger.Information("Set DNS zone '{dnsZone}' CNAME '{cname}' to Traffic Manager '{tmFqdn}'.", result.DnsZone.Id, "www", result.GlobalTrafficManager.Fqdn);
                await result.DnsZone.Update()
                    .DefineCNameRecordSet("www")
                    .WithAlias(result.GlobalTrafficManager.Fqdn).WithTimeToLive(600)
                    .Attach()
                    .ApplyAsync();

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

                    if (partnerCredentialUpdateConfig != null)
                    {
                        _logger.Information("Storing Partner Credentials in global key vault.");
                        await UpdatePartnerCredentialsAsync(secretPrefix, partnerCredentialUpdateConfig, kvValet);
                    }
                }

                if (addGlobalDB)
                {
                    var cosmosName = namingContext.CosmosDBName(baseName);
                    var dbId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosName}";
                    var db = await liftrAzure.GetCosmosDBAsync(dbId);
                    if (db == null)
                    {
                        db = await liftrAzure.CreateCosmosDBAsync(namingContext.Location, rgName, cosmosName, namingContext.Tags);
                        await liftrAzure.ExportDiagnosticsToLogAnalyticsAsync(db, logAnalyticsWorkspaceId);
                        _logger.Information("Created CosmosDB with Id {ResourceId}", db.Id);
                    }

                    result.GlobalCosmosDBAccount = db;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{nameof(CreateOrUpdateGlobalRGAsync)} failed.");
                throw;
            }
        }

        private async Task UpdatePartnerCredentialsAsync(string secretPrefix, PartnerCredentialUpdateOptions partnerCredentialUpdateConfig, KeyVaultConcierge kvValet)
        {
            _logger.Information("Create Certificate in Global Keyvault {keyvaultUri} for authenticating Secure Credential Sharing App.", kvValet.VaultUri);
            await CreateCrdentialSharingCertificateAsync(kvValet, CertificateName.PartnerCredSharingAppCert, partnerCredentialUpdateConfig.CertificateSubjectName);
            var certBundle = await kvValet.GetCertAsync(CertificateName.PartnerCredSharingAppCert);
            if (certBundle != null)
            {
                var privateKeyBytes = Convert.FromBase64String(certBundle.Value);
                using var certificate = new X509Certificate2(privateKeyBytes);
                var partnerKVClient = KeyVaultClientFactory.FromClientIdAndCertificate(partnerCredentialUpdateConfig.MultiTenantAppId, certificate, partnerCredentialUpdateConfig.AadEndpoint, partnerCredentialUpdateConfig.PartnerTenantId);
                var secrets = await partnerKVClient.GetSecretsAsync(partnerCredentialUpdateConfig.PartnerKeyvaultEndpoint);
                _logger.Information("List all secrets from partner keyvault.");
                foreach (var secret in secrets)
                {
                    var secretBundle = await partnerKVClient.GetSecretAsync(partnerCredentialUpdateConfig.PartnerKeyvaultEndpoint, secret.Identifier.Name);
                    _logger.Information("Get secert with name: {secretName} from partner keyvault with version {version}", secret.Identifier.Name, secret.Identifier.Version);
                    await kvValet.SetSecretAsync($"{secretPrefix}-PartnerSecretOptions--{secret.Identifier.Name}", secretBundle.Value);
                    _logger.Information("Copied secert with name: {secretName} to global keyvault", secret.Identifier.Name);
                }
            }
            else
            {
                _logger.Error("Certificate {CertificateName} to authenticate Multi-Tenant Credential Sharing App is not present in the Keyvault.", CertificateName.PartnerCredSharingAppCert);
                throw new KeyVaultErrorException(message: "Certificate to authenticate Secure Sharing App is not present in the Keyvault.");
            }
        }
    }
}
