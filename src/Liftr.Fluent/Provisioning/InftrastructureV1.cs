//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InftrastructureV1
    {
        private readonly ILiftrAzure _azure;
        private readonly ILogger _logger;

        public InftrastructureV1(ILiftrAzure azureClient, ILogger logger)
        {
            _azure = azureClient;
            _logger = logger;
        }

        public async Task<(IResourceGroup dataResourceGroup, IResourceGroup computeResourceGroup)> CreateDataAndComputeAsync(InfraV1Options options, NamingContext context = null)
        {
            options.CheckValid();

            if (context == null)
            {
                context = new NamingContext(options.PartnerName, options.ShortPartnerName, options.Environment, options.Location);
            }

            (var dataRG, string keyVaultResourceId) = await CreateDataResourceGroupAsync(options.DataCoreName, context, options.CosmosSecreteName, options);
            var computeRG = await CreateComputeGroupAsync(options.ComputeCoreName, context, options.WebAppTier, options.AspNetEnv, keyVaultResourceId, options);

            await _azure.RemoveAccessPolicyAsync(keyVaultResourceId, options.SPNObjectId);
            _logger.Information("Removed Key Vault access policy for the SDK writer {@ServicePrincipalObjectId}", options.SPNObjectId);

            return (dataRG, computeRG);
        }

        public async Task<(IResourceGroup rg, string keyVaultResourceId)> CreateDataResourceGroupAsync(string coreName, NamingContext context, string cosmosSecreteName, InfraV1Options options)
        {
            var rgName = context.ResourceGroupName(coreName);
            var kvName = context.KeyVaultName(coreName);
            var cosmosName = context.CosmosDBName(coreName);

            _logger.Information("Creating Resource Group ...");
            var rg = await _azure.CreateResourceGroupAsync(context.Location, rgName, context.Tags);
            _logger.Information($"Created Resource Group with Id {rg.Id}");

            _logger.Information("Creating Key Vault ...");
            var kv = await _azure.CreateKeyVaultAsync(context.Location, rgName, kvName, context.Tags, options.SPNClientId);
            _logger.Information($"Created KeyVault with Id {kv.Id}");

            _logger.Information("Creating CosmosDB ...");
            (var db, string connectionString) = await _azure.CreateCosmosDBAsync(context.Location, rgName, cosmosName, context.Tags);
            _logger.Information($"Created CosmosDB with Id {db.Id}");

            using (var valet = new KeyVaultConcierge(kv.VaultUri, options.SPNClientId, options.SPNClientSecret, _logger))
            {
                _logger.Information("Puting the CosmosDB Connection String in the key vault ...");
                await valet.SetSecretAsync(cosmosSecreteName, connectionString, context.Tags);

                _logger.Information("Creating AME management certificate in Key Vault with name {@certName} ...", options.ClientCert.CertificateName);
                var certIssuerName = "one-cert-issuer";
                await valet.SetCertificateIssuerAsync(certIssuerName, "OneCert");
                await valet.CreateCertificateAsync(options.ClientCert.CertificateName, certIssuerName, options.ClientCert.SubjectName, options.ClientCert.SubjectAlternativeNames, context.Tags);
                _logger.Information("Finished creating AME management certificate in Key Vault with name {@certName}", options.ClientCert.CertificateName);
            }

            _logger.Information($"Finished creating data resource group with Id {rg.Id}");

            return (rg, kv.Id);
        }

        public async Task<IResourceGroup> CreateComputeGroupAsync(string coreName, NamingContext context, PricingTier tier, string aspNetEnv, string kvResourceId, InfraV1Options options)
        {
            var rgName = context.ResourceGroupName(coreName);
            var webAppName = context.WebAppName(coreName);

            _logger.Information("Creating Resource Group ...");
            var rg = await _azure.CreateResourceGroupAsync(context.Location, rgName, context.Tags);
            _logger.Information($"Created Resource Group with Id {rg.Id}");

            _logger.Information("Creating Web App ...");
            var webApp = await _azure.CreateWebAppAsync(context.Location, rgName, webAppName, context.Tags, tier, aspNetEnv);
            _logger.Information($"Created Web App with Id {webApp.Id}");

            _logger.Information("Updating key vault to allow the web app to access ...");
            var kv = await _azure.GetKeyVaultByIdAsync(kvResourceId);
            if (kv == null)
            {
                var errMsg = $"Cannot find an existing Key Vault with the resource Id: {kvResourceId}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            kv = await kv.Update()
                .DefineAccessPolicy()
                    .ForObjectId(webApp.SystemAssignedManagedServiceIdentityPrincipalId)
                    .AllowSecretPermissions(SecretPermissions.List, SecretPermissions.Get)
                    .Attach()
                 .ApplyAsync();

            using (var valet = new KeyVaultConcierge(kv.VaultUri, options.SPNClientId, options.SPNClientSecret, _logger))
            {
                _logger.Information("Uploading ANT geneva configurations ...");
                var cert = await valet.DownloadCertAsync(options.ClientCert.CertificateName);
                await _azure.DeployGenevaToAppServicePlanAsync(webApp.AppServicePlanId, options.MDSOptions, cert.Value);
                _logger.Information("Finished uploading ANT geneva configurations");
            }

            await webApp.RestartAsync();

            _logger.Information($"Finished creating compute resource group with Id {rg.Id}");

            return rg;
        }
    }
}
