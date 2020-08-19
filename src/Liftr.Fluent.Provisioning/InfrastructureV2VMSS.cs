//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public partial class InfrastructureV2
    {
        private const string c_certDiskLocation = "/var/lib/waagent/Microsoft.Azure.KeyVault";

        public async Task<ProvisionedVMSSResources> CreateOrUpdateRegionalVMSSRGAsync(
            NamingContext namingContext,
            RegionalComputeOptions computeOptions,
            VMSSMachineInfo machineInfo,
            GenevaOptions genevaOptions,
            KeyVaultClient _kvClient,
            IPPoolManager ipPool,
            bool enableVNet,
            IEnumerable<string> certificateNameList = null)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (computeOptions == null)
            {
                throw new ArgumentNullException(nameof(computeOptions));
            }

            if (machineInfo == null)
            {
                throw new ArgumentNullException(nameof(machineInfo));
            }

            if (ipPool == null)
            {
                throw new ArgumentNullException(nameof(ipPool));
            }

            if (genevaOptions == null)
            {
                throw new ArgumentNullException(nameof(genevaOptions));
            }

            genevaOptions.CheckValid();

            ProvisionedVMSSResources provisionedResources = new ProvisionedVMSSResources();

            _logger.Information("InfraV2RegionalComputeOptions: {@InfraV2RegionalComputeOptions}", computeOptions);
            _logger.Information("MachineInfo: {@machineInfo}", machineInfo);
            computeOptions.CheckValues();
            machineInfo.CheckValues();

            _logger.Information("VMSS machine type: {AKSMachineType}", machineInfo.VMSize);
            _logger.Information("VMSS machine count: {AKSMachineCount}", machineInfo.MachineCount);
            _logger.Information("VMSS image: {GalleryImageVersionId}", machineInfo.GalleryImageVersionId);

            namingContext.Tags[nameof(machineInfo.GalleryImageVersionId)] = machineInfo.GalleryImageVersionId;

            var rgName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
            var vmssName = namingContext.VMSSName(computeOptions.ComputeBaseName);
            var currentPublicIP = await MetadataHelper.GetPublicIPAddressAsync();
            _logger.Information("Current public IP address: {currentPublicIP}", currentPublicIP);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            provisionedResources.ResourceGroup = await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);
            var dataRGName = namingContext.ResourceGroupName(computeOptions.DataBaseName);

            var vnetName = namingContext.NetworkName(computeOptions.DataBaseName);
            provisionedResources.VNet = await liftrAzure.GetVNetAsync(dataRGName, vnetName);
            if (provisionedResources.VNet == null)
            {
                var errMsg = $"Cannot find the VNet with name '{vnetName}' in Resource Group '{dataRGName}'.";
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var msiName = namingContext.MSIName(computeOptions.DataBaseName);
            provisionedResources.ManagedIdentity = await liftrAzure.GetMSIAsync(namingContext.ResourceGroupName(computeOptions.DataBaseName), msiName);
            if (provisionedResources.ManagedIdentity == null)
            {
                var errMsg = "Cannot find regional MSI with resource name: " + msiName;
                var ex = new InvalidOperationException(errMsg);
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var kvName = namingContext.KeyVaultName(computeOptions.DataBaseName);
            provisionedResources.RegionalKeyVault = await liftrAzure.GetKeyVaultAsync(dataRGName, kvName);
            if (provisionedResources.RegionalKeyVault == null)
            {
                var ex = new InvalidOperationException("Cannot find regional key vault with resource name: " + kvName);
                _logger.Error("Cannot find regional key vault with resource name: {ResourceName}", kvName);
                throw ex;
            }

            provisionedResources.GlobalKeyVault = await liftrAzure.GetKeyVaultByIdAsync(computeOptions.GlobalKeyVaultResourceId);
            if (provisionedResources.GlobalKeyVault == null)
            {
                var ex = new InvalidOperationException("Cannot find central key vault with resource Id: " + computeOptions.GlobalKeyVaultResourceId);
                _logger.Error("Cannot find central key vault with resource Id: {ResourceId}", computeOptions.GlobalKeyVaultResourceId);
                throw ex;
            }

            string sshUserName = null;
            string sshPublicKey = null;
            using (var globalKVValet = new KeyVaultConcierge(provisionedResources.GlobalKeyVault.VaultUri, _kvClient, _logger))
            {
                sshUserName = (await globalKVValet.GetSecretAsync(SSHUserNameSecretName))?.Value ?? throw new InvalidOperationException("Cannot find ssh user name in key vault");
                sshPublicKey = (await globalKVValet.GetSecretAsync(SSHPublicKeySecretName))?.Value ?? throw new InvalidOperationException("Cannot find ssh public key in key vault");
            }

            var certList = new List<string>();
            if (certificateNameList != null && certificateNameList.Any())
            {
                using var regionalKVValet = new KeyVaultConcierge(provisionedResources.RegionalKeyVault.VaultUri, _kvClient, _logger);

                foreach (var certName in certificateNameList)
                {
                    var bundle = await regionalKVValet.GetCertAsync(certName);
                    if (bundle == null)
                    {
                        var ex = new InvalidOperationException($"Cannot find certificate with name '{certName}' in Key Vault '{provisionedResources.RegionalKeyVault.VaultUri}'.");
                        _logger.Error(ex, ex.Message);
                        throw ex;
                    }

                    certList.Add($"{provisionedResources.RegionalKeyVault.VaultUri}secrets/{certName}");
                }

                _logger.Information("Key vault VM extension managed certificate list: {@certList}", certList);
            }

            provisionedResources.Subnet = await liftrAzure.CreateNewSubnetAsync(provisionedResources.VNet, namingContext.SubnetName(computeOptions.ComputeBaseName));

            if (enableVNet)
            {
                _logger.Information("Restrict the Key Vault '{kvId}' to IP '{currentPublicIP}' and subnet '{subnetId}'.", provisionedResources.RegionalKeyVault.Id, currentPublicIP, provisionedResources.Subnet?.Inner?.Id);
                await liftrAzure.WithKeyVaultAccessFromNetworkAsync(provisionedResources.RegionalKeyVault, currentPublicIP, provisionedResources.Subnet?.Inner?.Id);

                var storName = namingContext.StorageAccountName(computeOptions.DataBaseName);
                var stor = await liftrAzure.GetStorageAccountAsync(dataRGName, storName);
                if (stor != null)
                {
                    _logger.Information("Restrict access to storage account with Id '{storId}' to subnet '{subnetId}'.", stor.Id, provisionedResources.Subnet.Inner.Id);
                    await stor.Update().WithAccessFromNetworkSubnet(provisionedResources.Subnet.Inner.Id).ApplyAsync();
                }

                var dbName = namingContext.CosmosDBName(computeOptions.DataBaseName);
                var db = await liftrAzure.GetCosmosDBAsync(dataRGName, dbName);
                if (db != null)
                {
                    // The cosmos DB service endpoint PUT is not idempotent. PUT the same subnet Id will generate 400.
                    var dbVNetRules = db.VirtualNetworkRules;
                    if (dbVNetRules?.Any((subnetId) => subnetId?.Id?.OrdinalEquals(provisionedResources.Subnet.Inner.Id) == true) != true)
                    {
                        _logger.Information("Restrict access to cosmos DB with Id '{cosmosDBId}' to subnet '{subnetId}'.", db.Id, provisionedResources.Subnet.Inner.Id);
                        await db.Update().WithVirtualNetworkRule(provisionedResources.VNet.Id, provisionedResources.Subnet.Name).ApplyAsync();
                    }
                }
            }

            var lbName = namingContext.PublicLoadBalancerName(computeOptions.ComputeBaseName);
            provisionedResources.LoadBalancer = await liftrAzure.FluentClient.LoadBalancers.GetByResourceGroupAsync(rgName, lbName);

            if (provisionedResources.LoadBalancer != null)
            {
                provisionedResources.ClusterIP = await liftrAzure.FluentClient.PublicIPAddresses.GetByIdAsync(provisionedResources.LoadBalancer.PublicIPAddressIds[0]);
            }

            provisionedResources.VMSS = await liftrAzure.FluentClient.VirtualMachineScaleSets.GetByResourceGroupAsync(rgName, vmssName);

            var imgVersionId = new ResourceId(machineInfo.GalleryImageVersionId);

            // We need to set the customization information in the VMSS tags.
            // Within the VMSS instance, the application can retrieve those information from the instance Metadata service.
            var tags = new Dictionary<string, string>(namingContext.Tags)
            {
                ["ENV_" + nameof(ComputeTagMetadata.VaultEndpoint)] = provisionedResources.RegionalKeyVault.VaultUri,
                ["ENV_VaultName"] = provisionedResources.RegionalKeyVault.Name,
                ["ENV_" + nameof(ComputeTagMetadata.ASPNETCORE_ENVIRONMENT)] = namingContext.Environment.ToString(),
                ["ENV_" + nameof(ComputeTagMetadata.DOTNET_ENVIRONMENT)] = namingContext.Environment.ToString(),
                ["ENV_" + nameof(ComputeTagMetadata.GCS_REGION)] = namingContext.Location.Name,
                ["ENV_MONITORING_GCS_REGION"] = namingContext.Location.Name,
                ["ENV_" + nameof(genevaOptions.MONITORING_GCS_ENVIRONMENT)] = genevaOptions.MONITORING_GCS_ENVIRONMENT,
                ["ENV_" + nameof(genevaOptions.MONITORING_GCS_ACCOUNT)] = genevaOptions.MONITORING_GCS_ACCOUNT,
                ["ENV_" + nameof(genevaOptions.MONITORING_GCS_NAMESPACE)] = genevaOptions.MONITORING_GCS_NAMESPACE,
                ["ENV_" + nameof(genevaOptions.MONITORING_CONFIG_VERSION)] = genevaOptions.MONITORING_CONFIG_VERSION,
                ["ENV_" + nameof(genevaOptions.MDM_ACCOUNT)] = genevaOptions.MDM_ACCOUNT,
                ["ENV_" + nameof(genevaOptions.MDM_NAMESPACE)] = genevaOptions.MDM_NAMESPACE,
                ["ENV_" + nameof(genevaOptions.MDM_ENDPOINT)] = genevaOptions.MDM_ENDPOINT,
                ["ENV_VMSS_NAME"] = vmssName,
                ["ENV_IMG_NAME"] = imgVersionId.ChildResourceName,
            };

            if (provisionedResources.VMSS != null)
            {
                _logger.Information($"Using the existing VMSS with Id: {provisionedResources.VMSS.Id}. Updating tags ...");
                await provisionedResources.VMSS.Update().WithTags(tags).ApplyAsync();
                return provisionedResources;
            }

            var lbFrontendName = "publicFrontend";
            var lbBackendPoolName = "plbBackendPool";
            var lbSshNat = "ssh-nat";

            if (provisionedResources.LoadBalancer == null)
            {
                provisionedResources.ClusterIP = await ipPool.GetAvailableIPAsync(namingContext.Location);
                if (provisionedResources.ClusterIP == null)
                {
                    var errMsg = "Cannot find available IP address.";
                    var ex = new InvalidOperationException(errMsg);
                    _logger.Error(ex, ex.Message);
                    throw ex;
                }

                var lbProbName80 = "liveness-probe-80";
                var lbProbName443 = "liveness-probe-443";

                _logger.Information("Start creating new public load balancer with name: " + lbName);

                provisionedResources.LoadBalancer = await liftrAzure.FluentClient
                    .LoadBalancers
                    .Define(lbName)
                    .WithRegion(namingContext.Location)
                    .WithExistingResourceGroup(rgName)
                    .DefineLoadBalancingRule(lbName + "-tcp80")
                       .WithProtocol(TransportProtocol.Tcp)
                       .FromFrontend(lbFrontendName)
                       .FromFrontendPort(80)
                       .ToBackend(lbBackendPoolName)
                       .ToBackendPort(80)
                       .WithProbe(lbProbName80)
                       .WithIdleTimeoutInMinutes(15)
                       .Attach()
                    .DefineLoadBalancingRule(lbName + "-tcp443")
                       .WithProtocol(TransportProtocol.Tcp)
                       .FromFrontend(lbFrontendName)
                       .FromFrontendPort(443)
                       .ToBackend(lbBackendPoolName)
                       .ToBackendPort(443)
                       .WithProbe(lbProbName443)
                       .WithIdleTimeoutInMinutes(15)
                       .Attach()
                    .DefineInboundNatPool(lbSshNat)
                        .WithProtocol(TransportProtocol.Tcp)
                        .FromFrontend(lbFrontendName)
                        .FromFrontendPortRange(7000, 7200)
                        .ToBackendPort(22)
                        .Attach()
                    .WithSku(LoadBalancerSkuType.Standard)
                    .WithTags(namingContext.Tags)
                    .DefinePublicFrontend(lbFrontendName)
                        .WithExistingPublicIPAddress(provisionedResources.ClusterIP)
                        .Attach()
                    .DefineTcpProbe(lbProbName80)
                        .WithPort(80)
                        .WithIntervalInSeconds(10)
                        .WithNumberOfProbes(2)
                        .Attach()
                     .DefineTcpProbe(lbProbName443)
                        .WithPort(443)
                        .WithIntervalInSeconds(10)
                        .WithNumberOfProbes(2)
                        .Attach()
                    .CreateAsync();

                _logger.Information("Created Pulic Load Balancer with Id {publicLoadBalancerId}", provisionedResources.LoadBalancer.Id);
            }

            var nsgName = namingContext.NSGName(computeOptions.ComputeBaseName);
            var nsg = await liftrAzure.GetNSGAsync(rgName, nsgName);
            if (nsg == null)
            {
                // TODO: add the SSH rules here.
                nsg = await liftrAzure.GetOrCreateDefaultNSGAsync(namingContext.Location, rgName, nsgName, namingContext.Tags);
            }

            var vmSku = VMSSSkuHelper.ParseSkuString(machineInfo.VMSize);

            var vmssCreatable = liftrAzure.FluentClient
                .VirtualMachineScaleSets
                .Define(vmssName)
                .WithRegion(namingContext.Location)
                .WithExistingResourceGroup(rgName)
                .WithSku(vmSku)
                .WithExistingPrimaryNetworkSubnet(provisionedResources.VNet, provisionedResources.Subnet.Name)
                .WithExistingPrimaryInternetFacingLoadBalancer(provisionedResources.LoadBalancer)
                .WithPrimaryInternetFacingLoadBalancerBackends(lbBackendPoolName)
                .WithPrimaryInternetFacingLoadBalancerInboundNatPools(lbSshNat)
                .WithoutPrimaryInternalLoadBalancer()
                .WithLinuxGalleryImageVersion(machineInfo.GalleryImageVersionId)
                .WithRootUsername(sshUserName)
                .WithSsh(sshPublicKey)
                .WithExistingUserAssignedManagedServiceIdentity(provisionedResources.ManagedIdentity)
                .WithCapacity(machineInfo.MachineCount)
                .WithBootDiagnostics()
                .WithExistingNetworkSecurityGroup(nsg)
                .WithTags(tags);

            if (certList != null && certList.Count > 0)
            {
                // https://docs.microsoft.com/en-us/azure/virtual-machines/extensions/key-vault-linux
                var secretsManagementSettings = new Dictionary<string, object>()
                {
                    ["pollingIntervalInS"] = "600", // polling interval in seconds
                    ["certificateStoreName"] = string.Empty, // It is ignored on Linux
                    ["linkOnRenewal"] = false, // Not available on Linux e.g.: false
                    ["certificateStoreLocation"] = c_certDiskLocation, // disk path where certificate is stored, default: "/var/lib/waagent/Microsoft.Azure.KeyVault"
                    ["requireInitialSync"] = true, // initial synchronization of certificates e..g: true
                    ["observedCertificates"] = certList, // list of KeyVault URIs representing monitored certificates, e.g.: "https://myvault.vault.azure.net/secrets/mycertificate"
                };

                vmssCreatable = vmssCreatable
                    .DefineNewExtension("KVVMExtensionForLinux")
                    .WithPublisher("Microsoft.Azure.KeyVault")
                    .WithType("KeyVaultForLinux")
                    .WithVersion("1.0")
                    .WithMinorVersionAutoUpgrade()
                    .WithPublicSetting("secretsManagementSettings", secretsManagementSettings)
                    .Attach();

                _logger.Information($"VMSS Key Vault extension is configured. It will automatically download the certificates to this location: '{c_certDiskLocation}', cert list: '{certificateNameList.ToJsonString()}'");
            }

            _logger.Information($"Start creating VMSS with name '{vmssName}', SKU '{machineInfo.VMSize}', image version '{machineInfo.GalleryImageVersionId}'...");
            provisionedResources.VMSS = await vmssCreatable.CreateAsync();

            _logger.Information("Created VM Scale Set with Id {resourceId}", provisionedResources.VMSS.Id);

            return provisionedResources;
        }

        public async Task<(IVirtualMachineScaleSet vmss, ILoadBalancer lb, IPublicIPAddress pip)> GetRegionalVMSSResourcesAsync(
            NamingContext namingContext,
            RegionalComputeOptions computeOptions)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            if (computeOptions == null)
            {
                throw new ArgumentNullException(nameof(computeOptions));
            }

            var rgName = namingContext.ResourceGroupName(computeOptions.ComputeBaseName);
            var vmssName = namingContext.VMSSName(computeOptions.ComputeBaseName);

            var liftrAzure = _azureClientFactory.GenerateLiftrAzure();

            await liftrAzure.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);

            var lbName = namingContext.PublicLoadBalancerName(computeOptions.ComputeBaseName);
            var publicLoadBalancer = await liftrAzure.FluentClient.LoadBalancers.GetByResourceGroupAsync(rgName, lbName);

            IPublicIPAddress pip = null;

            if (publicLoadBalancer != null)
            {
                pip = await liftrAzure.FluentClient.PublicIPAddresses.GetByIdAsync(publicLoadBalancer.PublicIPAddressIds[0]);
            }

            var vmss = await liftrAzure.FluentClient.VirtualMachineScaleSets.GetByResourceGroupAsync(rgName, vmssName);

            if (vmss != null)
            {
                _logger.Information("Using the existing VMSS with Id: " + vmss.Id);
                return (vmss, publicLoadBalancer, pip);
            }

            var ex = new InvalidOperationException("Cannot find the VMSS resources");
            _logger.Error(ex, ex.Message);
            throw ex;
        }
    }
}
