//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ACISProvision
    {
        public const string c_baseName = "acis";

        private const string AllowedAcisExtensions = nameof(AllowedAcisExtensions);

        private readonly ILiftrAzureFactory _azureClientFactory;
        private readonly KeyVaultClient _kvClient;
        private readonly Serilog.ILogger _logger;
        private readonly string _allowedAcisExtensions;

        public ACISProvision(
            ILiftrAzureFactory azureClientFactory,
            KeyVaultClient kvClient,
            Serilog.ILogger logger,
            string allowedAcisExtensions = "Liftr")
        {
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _allowedAcisExtensions = allowedAcisExtensions;
        }

        public async Task<IStorageAccount> ProvisionACISResourcesAsync(NamingContext namingContext)
        {
            if (namingContext == null)
            {
                throw new ArgumentNullException(nameof(namingContext));
            }

            using var ops = _logger.StartTimedOperation(nameof(ProvisionACISResourcesAsync));
            try
            {
                var rgName = namingContext.ResourceGroupName(c_baseName);
                var storName = namingContext.StorageAccountName(c_baseName);
                var kvName = namingContext.KeyVaultName(c_baseName);

                var az = _azureClientFactory.GenerateLiftrAzure();
                var rg = await az.GetOrCreateResourceGroupAsync(namingContext.Location, rgName, namingContext.Tags);

                var stor = await az.GetOrCreateStorageAccountAsync(namingContext.Location, rgName, storName, namingContext.Tags);
                var conn = await stor.GetPrimaryConnectionStringAsync();

                var kv = await az.GetOrCreateKeyVaultAsync(namingContext.Location, rgName, kvName, namingContext.Tags);
                await az.GrantSelfKeyVaultAdminAccessAsync(kv);

                using var kvValet = new KeyVaultConcierge(kv.VaultUri, _kvClient, _logger);

                await kvValet.SetSecretAsync("ACISStorConn", conn, namingContext.Tags);

                // https://genevamondocs.azurewebsites.net/actions/How%20Do%20I/keyvault.html
                if (!await kvValet.ContainsSecretAsync(AllowedAcisExtensions))
                {
                    await kvValet.SetSecretAsync(AllowedAcisExtensions, _allowedAcisExtensions, namingContext.Tags);
                }

                var acisObjectId = GetGenevaActionObjectId(namingContext.Environment, az);

                if (!string.IsNullOrEmpty(acisObjectId))
                {
                    await kv.Update()
                        .DefineAccessPolicy()
                        .ForObjectId(acisObjectId)
                        .AllowKeyPermissions(KeyPermissions.Get, KeyPermissions.Decrypt, KeyPermissions.Sign)
                        .AllowSecretPermissions(SecretPermissions.Get)
                        .Attach()
                        .ApplyAsync();

                    _logger.Information("Granted ACIS AAD APP access policy to key vault " + kv.Name);
                }

                return stor;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        private static string GetGenevaActionObjectId(EnvironmentType env, ILiftrAzure az)
        {
            // https://genevamondocs.azurewebsites.net/actions/How%20Do%20I/keyvault.html
            if (env == EnvironmentType.Dev
                || env == EnvironmentType.Test
                || env == EnvironmentType.DogFood)
            {
                // AcisTestAAD
                if (az.IsAMETenant())
                {
                    return "2d3f19bf-83a1-4923-9c1e-4461a782a0dd";
                }
                else if (az.IsMicrosoftTenant())
                {
                    return "dceea83f-c801-4351-82ef-096e5af7080b";
                }
            }
            else if (env == EnvironmentType.Production
               || env == EnvironmentType.Canary)
            {
                // AcisProductionAAD
                if (az.IsAMETenant())
                {
                    return "b92c9af0-05af-4d83-bbbc-b92c0e58eb30";
                }
                else if (az.IsMicrosoftTenant())
                {
                    return "7ec63ecf-e3b9-4be4-880f-93ea2402db8e";
                }
            }

            return null;
        }
    }
}
