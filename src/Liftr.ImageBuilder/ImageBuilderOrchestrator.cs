//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageBuilderOrchestrator
    {
        private const string c_SBISASSecretName = "SBISASToken";

        private readonly Serilog.ILogger _logger;
        private readonly EnvironmentOptions _envOptions;
        private readonly LiftrAzureFactory _azFactory;
        private readonly NamingContext _namingContext;
        private readonly string _rgName;
        private readonly string _galleryName;
        private readonly string _imageName;
        private readonly string _storageAccountName;
        private readonly ITimeSource _timeSource;

        public ImageBuilderOrchestrator(
            EnvironmentOptions envOptions,
            LiftrAzureFactory azFactory,
            NamingContext namingContext,
            string rgName,
            string galleryName,
            string imageName,
            string storageAccountName,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _envOptions = envOptions ?? throw new ArgumentNullException(nameof(envOptions));
            _azFactory = azFactory ?? throw new ArgumentNullException(nameof(azFactory));
            _namingContext = namingContext ?? throw new ArgumentNullException(nameof(namingContext));
            _rgName = rgName ?? throw new ArgumentNullException(nameof(rgName));
            _galleryName = galleryName ?? throw new ArgumentNullException(nameof(galleryName));
            _imageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
            _storageAccountName = storageAccountName ?? throw new ArgumentNullException(nameof(storageAccountName));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CreateOrUpdateInfraAsync(string kvName)
        {
            var liftrAzure = _azFactory.GenerateLiftrAzure();

            _logger.Information("Creating Resource Group ...");
            IResourceGroup rg = null;
            try
            {
                rg = await liftrAzure.CreateResourceGroupAsync(_namingContext.Location, _rgName, _namingContext.Tags);
                _logger.Information("Created Resource Group with Id {ResourceId}", rg.Id);
            }
            catch (DuplicateNameException ex)
            {
                _logger.Information("There exist a RG with the same name. Reuse it. {ExceptionDetail}", ex);
                rg = await liftrAzure.GetResourceGroupAsync(_rgName);
            }

            _logger.Information("Getting storage account with name {storageAccountName}", _storageAccountName);
            var storageAccount = await liftrAzure.FluentClient.StorageAccounts
                .GetByResourceGroupAsync(_rgName, _storageAccountName);

            if (storageAccount != null)
            {
                _logger.Information("Using existing storage account with {resourceId}", storageAccount.Id);
            }
            else
            {
                _logger.Information("Creating storage account with name {storageAccountName}", _storageAccountName);

                storageAccount = await liftrAzure.FluentClient.StorageAccounts
                .Define(_storageAccountName)
                .WithRegion(_namingContext.Location)
                .WithExistingResourceGroup(_rgName)
                .WithTags(_namingContext.Tags)
                .CreateAsync();

                _logger.Information("Created storage account with {resourceId}", storageAccount.Id);
            }

            var targetReousrceId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{_rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
            var kv = await liftrAzure.GetKeyVaultByIdAsync(targetReousrceId);
            if (kv == null)
            {
                _logger.Information("Creating Key Vault ...");
                kv = await liftrAzure.CreateKeyVaultAsync(_namingContext.Location, _rgName, kvName, _namingContext.Tags, _envOptions.ProvisioningRunnerClientId);
                _logger.Information("Created KeyVault with Id {ResourceId}", kv.Id);
            }
            else
            {
                _logger.Information("Use existing Key Vault with Id {ResourceId}.", kv.Id);

                await kv.Update()
                .DefineAccessPolicy()
                    .ForServicePrincipal(_envOptions.ProvisioningRunnerClientId)
                    .AllowSecretAllPermissions()
                    .AllowCertificateAllPermissions()
                    .Attach()
                .ApplyAsync();
            }

            try
            {
                _logger.Information("Granting resource group's contributor role to Azure Image Builder First Party app ...");
                await liftrAzure.Authenticated.RoleAssignments
                .Define(SdkContext.RandomGuid())
                .ForObjectId(_envOptions.AzureVMImageBuilderObjectId)
                .WithBuiltInRole(BuiltInRole.Contributor)
                .WithResourceGroupScope(rg)
                .CreateAsync();
                _logger.Information("Granted resource group's contributor role to Azure Image Builder First Party app.");
            }
            catch (CloudException ex) when (ex.Message.Contains("The role assignment already exists", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Information("There exists the same role assignment.");
            }

            ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
            var gallery = await galleryClient.CreateGalleryAsync(liftrAzure.FluentClient, _namingContext.Location, _rgName, _galleryName, _namingContext.Tags);

            var galleryImageDefinition = await galleryClient.CreateImageDefinitionAsync(liftrAzure.FluentClient, _namingContext.Location, _rgName, _galleryName, _imageName, _namingContext.Tags);

            _logger.Information("Successfully finished CreateOrUpdateInfraAsync.");
        }

        public async Task MoveSBIToOurStorageAsync(string kvName, KeyVaultClient kvClient)
        {
            var liftrAzure = _azFactory.GenerateLiftrAzure();
            var kvId = $"subscriptions/{liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{_rgName}/providers/Microsoft.KeyVault/vaults/{kvName}";
            var kv = await liftrAzure.GetKeyVaultByIdAsync(kvId);
            if (kv == null)
            {
                var errMsg = "Cannot find the Key Vault with Id: " + kvId;
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            using (var kvValet = new KeyVaultConcierge(kv.VaultUri, kvClient, _logger))
            {
                string sbiRegistryDownloadToken = null;
                try
                {
                    sbiRegistryDownloadToken = (await kvValet.GetSecretAsync(c_SBISASSecretName)).Value;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Cannot download the secret with name {c_SBISASSecretName}");
                    throw;
                }

                var artifactStorageAcct = await GetArtifactStorageAccountAsync();
                var mover = new SBIVHDMover(_logger);
                var vhds = await mover.CopyRegistryInfoAsync(sbiRegistryDownloadToken, _envOptions.SBIMoverOptions, artifactStorageAcct);

                foreach (var tup in vhds)
                {
                    var sbiVersionName = tup.Item1;
                    var vhdUrl = tup.Item2;

                    var customImage = await CreateCustomImageAsync(sbiVersionName, vhdUrl);
                    ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
                    var sbiImgDefinition = await galleryClient.CreateImageDefinitionAsync(liftrAzure.FluentClient, _namingContext.Location, _rgName, _galleryName, sbiVersionName, _namingContext.Tags);

                    var tags = new Dictionary<string, string>(_namingContext.Tags);
                    tags["srcVersion"] = sbiVersionName;
                    tags["imgCreatedAt"] = DateTime.UtcNow.ToZuluString();
                    await galleryClient.CreateImageVersionAsync(liftrAzure.FluentClient, _namingContext.Location.ToString(), _rgName, _galleryName, sbiVersionName, customImage, tags);
                }
            }
        }

        public async Task<string> UploadArtifactAndPrepareBuilderTemplateAsync(ArtifactStoreOptions storeOptions, string artifactPath, string imageMetaPath, string aibTemplatePath, string sbiVersion)
        {
            if (storeOptions == null)
            {
                throw new ArgumentNullException(nameof(storeOptions));
            }

            if (!File.Exists(artifactPath))
            {
                var errMsg = $"Cannot find the artifact file located at {artifactPath}";
                _logger.Error(errMsg);
                throw new FileNotFoundException(errMsg);
            }

            if (!File.Exists(imageMetaPath))
            {
                var errMsg = $"Cannot find 'image-meta.json' file located at {imageMetaPath}";
                _logger.Error(errMsg);
                throw new FileNotFoundException(errMsg);
            }

            if (!File.Exists(aibTemplatePath))
            {
                var errMsg = $"Cannot find 'image-builder-sbi.template.base.json' file located at {aibTemplatePath}";
                _logger.Error(errMsg);
                throw new FileNotFoundException(errMsg);
            }

            var imageMetaContent = File.ReadAllText(imageMetaPath);
            var imageMeta = JsonConvert.DeserializeObject<ImageMetaInfo>(imageMetaContent);

            if (string.IsNullOrEmpty(imageMeta.BuildTag))
            {
                var errMsg = $"Cannot parse the content of the {imageMetaPath}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            _logger.Information("Corresponding docker image info: {@imageMeta}", imageMeta);

            ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
            var imgVersion = await galleryClient.GetImageVersionAsync(_azFactory.GenerateLiftrAzure().FluentClient, _rgName, _galleryName, sbiVersion);
            if (imgVersion == null)
            {
                var errMsg = "Cannot find the source SBI image version.";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            var store = new ArtifactStore(storeOptions, _timeSource, _logger);

            var storageAccount = await GetArtifactStorageAccountAsync();
            _logger.Information("retrieved the storage account with name {storageAccountName}", _storageAccountName);

            var artifactUrlWithSAS = await store.UploadFileAndGenerateReadSASAsync(storageAccount, artifactPath);
            _logger.Information("uploaded the file {filePath} and generated the url with the SAS token.", artifactPath);

            var generatedTemplate = GenerateImageTemplate(aibTemplatePath, artifactUrlWithSAS, imageMeta, imgVersion.Id, _namingContext.Location);
            return generatedTemplate;
        }

        #region Private
        private async Task<CloudStorageAccount> GetArtifactStorageAccountAsync()
        {
            var liftrAzure = _azFactory.GenerateLiftrAzure();
            _logger.Information("Getting storage account with name {storageAccountName}", _storageAccountName);
            var storageAccount = await liftrAzure.FluentClient.StorageAccounts
                .GetByResourceGroupAsync(_rgName, _storageAccountName);

            if (storageAccount == null)
            {
                _logger.Error("Cannot find the storage with name {storageAccountName} in the resource group {rgName}", _storageAccountName, _rgName);
                throw new InvalidOperationException("Cannot find storage account with name: " + _storageAccountName);
            }

            var keys = await storageAccount.GetKeysAsync();
            var key = keys[0];
            var cred = new StorageCredentials(_storageAccountName, key.Value, key.KeyName);
            return new CloudStorageAccount(cred, useHttps: true);
        }

        private string GenerateImageTemplate(string aibTemplatePath, string artifactUrlWithSAS, ImageMetaInfo imageMeta, string imgVersionId, Region location)
        {
            var templateContent = File.ReadAllText(aibTemplatePath);
            templateContent = templateContent.Replace("ARTIFACT_URI_PLACEHOLDER", artifactUrlWithSAS, StringComparison.OrdinalIgnoreCase);
            dynamic obj = JObject.Parse(templateContent);

            var subscription = _azFactory.GenerateLiftrAzure().FluentClient.SubscriptionId;
            var galleryImageResourceId = $"/subscriptions/{subscription}/resourceGroups/{_rgName}/providers/Microsoft.Compute/galleries/{_galleryName}/images/{_imageName}";
            _logger.Information("Azure VM Image builder will generate a new version of the image with resource Id {galleryImageResourceId}", galleryImageResourceId);

            obj.location = location.Name;
            obj.properties.source.imageVersionId = imgVersionId;

            var galleryTarget = obj.properties.distribute[0];
            galleryTarget.galleryImageId = galleryImageResourceId;

            var artifactTags = galleryTarget.artifactTags;
            artifactTags[nameof(imageMeta.CommitId)] = imageMeta.CommitId;
            artifactTags[nameof(imageMeta.TimeStamp)] = imageMeta.TimeStamp;
            artifactTags[nameof(imageMeta.ImageId)] = imageMeta.ImageId;
            artifactTags[nameof(imageMeta.BuildTag)] = imageMeta.BuildTag;

            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        private async Task<IVirtualMachineCustomImage> CreateCustomImageAsync(string name, string vhdUrl)
        {
            var azureLiftr = _azFactory.GenerateLiftrAzure();

            var customImage = await azureLiftr.FluentClient.VirtualMachineCustomImages
                .GetByResourceGroupAsync(_rgName, name);

            if (customImage == null)
            {
                _logger.Information("Start creating custom image {name} from vhd Url: {vhdUrl}", name, vhdUrl);
                customImage = await azureLiftr.FluentClient.VirtualMachineCustomImages
                    .Define(name)
                    .WithRegion(_namingContext.Location)
                    .WithExistingResourceGroup(_rgName)
                    .WithLinuxFromVhd(vhdUrl, OperatingSystemStateTypes.Generalized)
                    .WithTags(_namingContext.Tags)
                    .CreateAsync();
                _logger.Information("Created a custom image with resource Id: {customImageId}", customImage.Id);
            }
            else
            {
                _logger.Information("Use the exisitng custom image with resource Id: {customImageId}", customImage.Id);
            }

            return customImage;
        }
        #endregion
    }
}
