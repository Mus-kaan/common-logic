//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Hosting;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Rest.Azure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public sealed partial class ActionExecutor : IHostedService
    {
        private async Task ManageComputeResourcesAsync(HostingEnvironmentOptions targetOptions, KeyVaultClient kvClient, LiftrAzureFactory azFactory)
        {
            var liftrAzure = azFactory.GenerateLiftrAzure();
            var infra = new InfrastructureV2(azFactory, kvClient, _logger);
            var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, targetOptions.EnvironmentName, targetOptions.Global.Location);
            var globalRGName = globalNamingContext.ResourceGroupName(targetOptions.Global.BaseName);
            File.WriteAllText("global-vault-name.txt", globalNamingContext.KeyVaultName(targetOptions.Global.BaseName));

            IPPoolManager ipPool = null;
            if (targetOptions.IPPerRegion > 0)
            {
                var ipNamePrefix = globalNamingContext.GenerateCommonName(targetOptions.Global.BaseName, noRegion: true);
                var poolRG = ipNamePrefix + "-ip-pool-rg";
                ipPool = new IPPoolManager(poolRG, ipNamePrefix, azFactory, _logger);
            }

            var parsedRegionInfo = GetRegionalOptions(targetOptions);
            var regionOptions = parsedRegionInfo.RegionOptions;
            var regionalNamingContext = parsedRegionInfo.RegionNamingContext;
            var aksRGName = parsedRegionInfo.AKSRGName;
            var aksName = parsedRegionInfo.AKSName;
            regionalNamingContext.Tags["GlobalRG"] = globalRGName;

            RegionalComputeOptions regionalComputeOptions = new RegionalComputeOptions()
            {
                DataBaseName = regionOptions.DataBaseName,
                ComputeBaseName = regionOptions.ComputeBaseName ?? parsedRegionInfo.ComputeRegionOptions.ComputeBaseName,
                GlobalKeyVaultResourceId = $"subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                LogAnalyticsWorkspaceResourceId = targetOptions.LogAnalyticsWorkspaceId,
                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                SecretPrefix = _hostingOptions.SecretPrefix,
                GlobalStorageResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}",
                DomainName = targetOptions.DomainName,
                OneCertCertificates = targetOptions.OneCertCertificates,
            };

            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);

            var dataOptions = new RegionalDataOptions()
            {
                ActiveDBKeyName = _commandOptions.ActiveKeyName,
                SecretPrefix = _hostingOptions.SecretPrefix,
                OneCertCertificates = targetOptions.OneCertCertificates,
                DataPlaneSubscriptions = regionOptions.DataPlaneSubscriptions,
                DataPlaneStorageCountPerSubscription = _hostingOptions.StorageCountPerDataPlaneSubscription,
                EnableVNet = targetOptions.EnableVNet,
                DBSupport = _hostingOptions.DBSupport,
                GlobalKeyVaultResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.KeyVault/vaults/{globalNamingContext.KeyVaultName(targetOptions.Global.BaseName)}",
                GlobalStorageResourceId = $"/subscriptions/{targetOptions.AzureSubscription}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}",
                LogAnalyticsWorkspaceId = targetOptions.LogAnalyticsWorkspaceId,
                DomainName = targetOptions.DomainName,
                DNSZoneId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Network/dnszones/{targetOptions.DomainName}",
            };

            bool createVNet = targetOptions.IsAKS ? targetOptions.EnableVNet : true;

            regionalNamingContext.Tags["DataRG"] = regionalNamingContext.ResourceGroupName(regionOptions.DataBaseName);

            var acr = await infra.GetACRAsync(targetOptions.Global.BaseName, globalNamingContext);
            if (acr == null)
            {
                var errMsg = "Cannot find the global ACR.";
                _logger.Fatal(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            if (string.IsNullOrEmpty(targetOptions.DiagnosticsStorageId))
            {
                targetOptions.DiagnosticsStorageId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{globalRGName}/providers/Microsoft.Storage/storageAccounts/{globalNamingContext.StorageAccountName(targetOptions.Global.BaseName)}";
            }

            await GetDiagnosticsStorageAccountAsync(azFactory, targetOptions.DiagnosticsStorageId);

            if (targetOptions.AKSConfigurations != null)
            {
                ProvisionedComputeResources computeResources = null;
                if (parsedRegionInfo.ComputeRegionOptions != null)
                {
                    computeResources = await infra.CreateOrUpdateComputeRegionAsync(
                        parsedRegionInfo.ComputeRegionNamingContext,
                        regionalNamingContext,
                        regionalComputeOptions,
                        targetOptions.AKSConfigurations,
                        kvClient,
                        targetOptions.EnableVNet);
                }
                else
                {
                    (var kv, var msi, var aks, var aksMIObjectId, var kubeletObjectId) = await infra.CreateOrUpdateRegionalAKSRGAsync(
                        regionalNamingContext,
                        regionalComputeOptions,
                        targetOptions.AKSConfigurations,
                        kvClient,
                        targetOptions.EnableVNet);

                    computeResources = new ProvisionedComputeResources()
                    {
                        KeyVault = kv,
                        ManagedIdentity = msi,
                        AKS = aks,
                        AKSObjectId = aksMIObjectId,
                        KubeletObjectId = kubeletObjectId,
                    };
                }

                try
                {
                    // ACR Pull
                    var roleDefinitionId = $"/subscriptions/{liftrAzure.FluentClient.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d";
                    _logger.Information("Granting ACR pull role to the kubelet MI over the acr '{acrLogin}' ...", acr.LoginServerUrl);
                    await liftrAzure.Authenticated.RoleAssignments
                        .Define(SdkContext.RandomGuid())
                        .ForObjectId(computeResources.KubeletObjectId)
                        .WithRoleDefinition(roleDefinitionId)
                        .WithResourceScope(acr)
                        .CreateAsync();
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }

                var pip = await WriteReservedIPToDiskAsync(azFactory, aksRGName, aksName, parsedRegionInfo.AKSRegion, targetOptions, ipPool);
                if (pip?.Name?.OrdinalContains(IPPoolManager.c_reservedNamePart) == true)
                {
                    try
                    {
                        _logger.Information("Granting the Network contrinutor over the public IP '{pipId}' to the AKS SPN with object Id '{AKSobjectId}' ...", pip.Id, computeResources.AKSObjectId);
                        await liftrAzure.Authenticated.RoleAssignments
                            .Define(SdkContext.RandomGuid())
                            .ForObjectId(computeResources.AKSObjectId)
                            .WithBuiltInRole(BuiltInRole.NetworkContributor)
                            .WithResourceScope(pip)
                            .CreateAsync();
                        _logger.Information("Granted Network contrinutor.");
                    }
                    catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                    {
                    }
                    catch (CloudException ex) when (ex.IsMissUseAppIdAsObjectId())
                    {
                        _logger.Error("The AKS SPN object Id '{AKSobjectId}' is the object Id of the Application. Please use the object Id of the Service Principal. Details: https://aka.ms/liftr/sp-objectid-vs-app-objectid", computeResources.AKSObjectId);
                        throw;
                    }
                }

                File.WriteAllText("vault-name.txt", computeResources.KeyVault.Name);
                File.WriteAllText("aks-domain.txt", $"{computeResources.AKS.Name}.{targetOptions.DomainName}");
                File.WriteAllText("aks-name.txt", computeResources.AKS.Name);
                File.WriteAllText("aks-rg.txt", computeResources.AKS.ResourceGroupName);
                File.WriteAllText("msi-resourceId.txt", computeResources.ManagedIdentity.Id);
                File.WriteAllText("msi-clientId.txt", computeResources.ManagedIdentity.ClientId);
            }
            else
            {
                var certNameList = targetOptions.OneCertCertificates.Select(kvp => kvp.Key).ToList();
                certNameList.Add(CertificateName.DefaultSSL);
                if (targetOptions.OneCertCertificates?.ContainsKey(CertificateName.GenevaClientCert) == true)
                {
                    targetOptions.Geneva.GENEVA_CERT_SAN = targetOptions.OneCertCertificates[CertificateName.GenevaClientCert];
                }

                var vmss = await infra.CreateOrUpdateRegionalVMSSRGAsync(
                    regionalNamingContext,
                    regionalComputeOptions,
                    targetOptions.VMSSConfigurations,
                    targetOptions.Geneva,
                    kvClient,
                    ipPool,
                    targetOptions.EnableVNet,
                    certNameList);

                if (SimpleDeployExtension.AfterProvisionRegionalVMSSResourcesAsync != null)
                {
                    using (_logger.StartTimedOperation(nameof(SimpleDeployExtension.AfterProvisionRegionalVMSSResourcesAsync)))
                    {
                        var parameters = new VMSSCallbackParameters()
                        {
                            CallbackConfigurations = _callBackConfigs,
                            BaseName = regionOptions.DataBaseName,
                            NamingContext = regionalNamingContext,
                            ComputeOptions = regionalComputeOptions,
                            RegionOptions = regionOptions,
                            Resources = vmss,
                        };

                        await SimpleDeployExtension.AfterProvisionRegionalVMSSResourcesAsync.Invoke(parameters);
                    }
                }
            }

            _logger.Information("-----------------------------------------------------------------------");
            _logger.Information($"Successfully finished managing regional compute resources.");
            _logger.Information("-----------------------------------------------------------------------");
        }
    }
}
