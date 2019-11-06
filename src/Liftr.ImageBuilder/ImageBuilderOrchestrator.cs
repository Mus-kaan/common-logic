//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.ImageBuilder.Tests")]

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageBuilderOrchestrator
    {
        private const string c_SBISASSecretName = "SBISASToken";

        private readonly Serilog.ILogger _logger;
        private readonly LiftrAzureFactory _azFactory;
        private readonly ITimeSource _timeSource;

        public ImageBuilderOrchestrator(
            LiftrAzureFactory azFactory,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _azFactory = azFactory ?? throw new ArgumentNullException(nameof(azFactory));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(IResourceGroup, IStorageAccount, IVault, IGallery, IGalleryImage)> CreateOrUpdateInfraAsync(ImageBuilderOptions imgOptions, string azureVMImageBuilderObjectId, string kvName)
        {
            if (imgOptions == null)
            {
                throw new ArgumentNullException(nameof(imgOptions));
            }

            imgOptions.CheckValid();

            var liftrAzure = _azFactory.GenerateLiftrAzure();

            var rg = await liftrAzure.GetOrCreateResourceGroupAsync(imgOptions.Location, imgOptions.ResourceGroupName, imgOptions.Tags);
            var storageAccount = await liftrAzure.GetOrCreateStorageAccountAsync(imgOptions.Location, imgOptions.ResourceGroupName, imgOptions.StorageAccountName, imgOptions.Tags);
            var kv = await liftrAzure.GetOrCreateKeyVaultAsync(imgOptions.Location, imgOptions.ResourceGroupName, kvName, imgOptions.Tags);
            await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(kv);

            try
            {
                _logger.Information("Granting resource group's contributor role to Azure Image Builder First Party app ...");
                await liftrAzure.Authenticated.RoleAssignments
                .Define(SdkContext.RandomGuid())
                .ForObjectId(azureVMImageBuilderObjectId)
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
            var gallery = await galleryClient.CreateGalleryAsync(liftrAzure.FluentClient, imgOptions.Location, imgOptions.ResourceGroupName, imgOptions.GalleryName, imgOptions.Tags);

            var galleryImageDefinition = await galleryClient.CreateImageDefinitionAsync(liftrAzure.FluentClient, imgOptions.Location, imgOptions.ResourceGroupName, imgOptions.GalleryName, imgOptions.ImageDefinitionName, imgOptions.Tags);

            _logger.Information("Successfully finished CreateOrUpdateInfraAsync.");
            return (rg, storageAccount, kv, gallery, galleryImageDefinition);
        }

        public async Task MoveSBIToOurStorageAsync(ImageBuilderOptions imgOptions, SBIMoverOptions moverOptions, string kvId, KeyVaultClient kvClient)
        {
            if (imgOptions == null)
            {
                throw new ArgumentNullException(nameof(imgOptions));
            }

            imgOptions.CheckValid();

            var liftrAzure = _azFactory.GenerateLiftrAzure();
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

                var artifactStorageAcct = await GetArtifactStorageAccountAsync(imgOptions);
                var mover = new SBIVHDMover(_logger);
                var vhds = await mover.CopyRegistryInfoAsync(sbiRegistryDownloadToken, moverOptions, artifactStorageAcct);

                foreach (var tup in vhds)
                {
                    var sbiVersionName = tup.Item1;
                    var vhdUrl = tup.Item2;

                    var customImage = await CreateCustomImageAsync(imgOptions, sbiVersionName, vhdUrl);
                    ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
                    var sbiImgDefinition = await galleryClient.CreateImageDefinitionAsync(liftrAzure.FluentClient, imgOptions.Location, imgOptions.ResourceGroupName, imgOptions.GalleryName, sbiVersionName, imgOptions.Tags);

                    var tags = new Dictionary<string, string>(imgOptions.Tags);
                    tags["srcVersion"] = sbiVersionName;
                    tags["imgCreatedAt"] = DateTime.UtcNow.ToZuluString();
                    await galleryClient.CreateImageVersionAsync(liftrAzure.FluentClient, imgOptions.Location.ToString(), imgOptions.ResourceGroupName, imgOptions.GalleryName, sbiVersionName, customImage, tags);
                }
            }
        }

        public async Task<string> BuildCustomizedSBIAsync(ImageBuilderOptions imgOptions, ArtifactStoreOptions storeOptions, string artifactPath, string imageMetaPath, string sbiVersion, CancellationToken cancellationToken)
        {
            if (imgOptions == null)
            {
                throw new ArgumentNullException(nameof(imgOptions));
            }

            imgOptions.CheckValid();

            ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
            var imgVersion = await galleryClient.GetImageVersionAsync(_azFactory.GenerateLiftrAzure().FluentClient, imgOptions.ResourceGroupName, imgOptions.GalleryName, sbiVersion);
            if (imgVersion == null)
            {
                var errMsg = "Cannot find the source SBI image version.";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            return await BuildCustomizedSBIImplAsync(imgOptions, storeOptions, artifactPath, imageMetaPath, imgVersion.Id, cancellationToken);
        }

        internal async Task<string> BuildCustomizedSBIImplAsync(ImageBuilderOptions imgOptions, ArtifactStoreOptions storeOptions, string artifactPath, string imageMetaPath, string srcImgVersionId, CancellationToken cancellationToken)
        {
            if (imgOptions == null)
            {
                throw new ArgumentNullException(nameof(imgOptions));
            }

            imgOptions.CheckValid();

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

            var imageMetaContent = File.ReadAllText(imageMetaPath);
            var imageMeta = JsonConvert.DeserializeObject<ImageMetaInfo>(imageMetaContent);

            if (string.IsNullOrEmpty(imageMeta.BuildTag))
            {
                var errMsg = $"Cannot parse the content of the {imageMetaPath}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            _logger.Information("Corresponding docker image info: {@imageMeta}", imageMeta);

            var store = new ArtifactStore(storeOptions, _timeSource, _logger);

            var storageAccount = await GetArtifactStorageAccountAsync(imgOptions);
            _logger.Information("retrieved the storage account with name {storageAccountName}", imgOptions.StorageAccountName);

            var cleanupCount = await store.CleanUpOldArtifactsAsync(storageAccount);
            _logger.Information("Removed old artifacts count: {cleanupCount}", cleanupCount);

            var artifactUrlWithSAS = await store.UploadBuildArtifactsAndGenerateReadSASAsync(storageAccount, artifactPath);
            _logger.Information("uploaded the file {filePath} and generated the url with the SAS token.", artifactPath);

            var templateName = imageMeta.BuildTag;
            var generatedTemplate = GenerateImageTemplate(imgOptions, artifactUrlWithSAS, imageMeta, srcImgVersionId, imgOptions.Location, templateName);

            ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
            var az = _azFactory.GenerateLiftrAzure();
            var cleanUpTask = CleanUpOldImageVersionsAsync(az.FluentClient, imgOptions, galleryClient);
            await galleryClient.CreateVMImageBuilderTemplateAsync(az, imgOptions.Location, imgOptions.ResourceGroupName, generatedTemplate);

            var res = await galleryClient.RunVMImageBuilderTemplateAsync(az, imgOptions.ResourceGroupName, templateName, cancellationToken);
            _logger.Information("Run AIB template result: {AIBTemplateRunResult}", res);

            await galleryClient.DeleteVMImageBuilderTemplateAsync(az, imgOptions.ResourceGroupName, templateName, cancellationToken);

            await cleanUpTask;

            return res;
        }

        #region Private
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Swallow clean up issues.")]
        private async Task CleanUpOldImageVersionsAsync(IAzure az, ImageBuilderOptions imgOptions, ImageGalleryClient galleryClient)
        {
            _logger.Information("Start cleaning up old image versions ...");
            var existingVersions = await galleryClient.ListImageVersionsAsync(az, imgOptions.ResourceGroupName, imgOptions.GalleryName, imgOptions.ImageDefinitionName);
            bool deletedAny = false;
            foreach (var version in existingVersions)
            {
                try
                {
                    var timeStamp = DateTime.Parse(version.Tags[NamingContext.c_createdAtTagName], CultureInfo.InvariantCulture);
                    var cutOffTime = _timeSource.UtcNow.AddDays(-1 * imgOptions.ImageVersionTTLInDays);
                    if (timeStamp < cutOffTime)
                    {
                        deletedAny = true;
                        _logger.Information("Remove old verion with '{versionId}' since it is older than the cutOffTime: {cutOffTime}", version.Id, cutOffTime);
                        await galleryClient.DeleteImageVersionAsync(az, imgOptions.ResourceGroupName, imgOptions.GalleryName, imgOptions.ImageDefinitionName, version.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Clean up old image verion failed for versionId: {versionId}", version.Id);
                }
            }

            if (!deletedAny)
            {
                _logger.Information("All image versions are within its TTL. Skip deleting.");
                return;
            }
        }

        private async Task<CloudStorageAccount> GetArtifactStorageAccountAsync(ImageBuilderOptions imgOptions)
        {
            var liftrAzure = _azFactory.GenerateLiftrAzure();
            _logger.Information("Getting storage account with name {storageAccountName}", imgOptions.StorageAccountName);
            var storageAccount = await liftrAzure.FluentClient.StorageAccounts
                .GetByResourceGroupAsync(imgOptions.ResourceGroupName, imgOptions.StorageAccountName);

            if (storageAccount == null)
            {
                _logger.Error("Cannot find the storage with name {storageAccountName} in the resource group {rgName}", imgOptions.StorageAccountName, imgOptions.ResourceGroupName);
                throw new InvalidOperationException("Cannot find storage account with name: " + imgOptions.StorageAccountName);
            }

            var keys = await storageAccount.GetKeysAsync();
            var key = keys[0];
            var cred = new StorageCredentials(imgOptions.StorageAccountName, key.Value, key.KeyName);
            return new CloudStorageAccount(cred, useHttps: true);
        }

        private string GenerateImageTemplate(ImageBuilderOptions imgOptions, string artifactUrlWithSAS, ImageMetaInfo imageMeta, string srcImgVersionId, Region location, string imageTemplateName)
        {
            var templateContent = EmbeddedContentReader.GetContent(Assembly.GetExecutingAssembly(), "Microsoft.Liftr.ImageBuilder.aib.template.base.json");
            templateContent = templateContent.Replace("ARTIFACT_URI_PLACEHOLDER", artifactUrlWithSAS, StringComparison.OrdinalIgnoreCase);
            dynamic templateObj = JObject.Parse(templateContent);
            var obj = templateObj.resources[0];

            var subscription = _azFactory.GenerateLiftrAzure().FluentClient.SubscriptionId;
            var galleryImageResourceId = $"/subscriptions/{subscription}/resourceGroups/{imgOptions.ResourceGroupName}/providers/Microsoft.Compute/galleries/{imgOptions.GalleryName}/images/{imgOptions.ImageDefinitionName}";
            _logger.Information("Azure VM Image builder will generate a new version of the image with resource Id {galleryImageResourceId}", galleryImageResourceId);

            obj.name = imageTemplateName;
            obj.location = location.Name;
            obj.properties.source.imageVersionId = srcImgVersionId;

            var galleryTarget = obj.properties.distribute[0];
            galleryTarget.galleryImageId = galleryImageResourceId;

            var artifactTags = galleryTarget.artifactTags;
            artifactTags[nameof(imageMeta.CommitId)] = imageMeta.CommitId;
            artifactTags["CDPx" + nameof(imageMeta.TimeStamp)] = imageMeta.TimeStamp;
            artifactTags["CDPx" + nameof(imageMeta.ImageId)] = imageMeta.ImageId;
            artifactTags["CDPx" + nameof(imageMeta.BuildTag)] = imageMeta.BuildTag;

            foreach (var kvp in imgOptions.Tags)
            {
                artifactTags[kvp.Key] = kvp.Value;
            }

            return JsonConvert.SerializeObject(templateObj, Formatting.Indented);
        }

        private async Task<IVirtualMachineCustomImage> CreateCustomImageAsync(ImageBuilderOptions imgOptions, string name, string vhdUrl)
        {
            var azureLiftr = _azFactory.GenerateLiftrAzure();

            var customImage = await azureLiftr.FluentClient.VirtualMachineCustomImages
                .GetByResourceGroupAsync(imgOptions.ResourceGroupName, name);

            if (customImage == null)
            {
                _logger.Information("Start creating custom image {name} from vhd Url: {vhdUrl}", name, vhdUrl);
                customImage = await azureLiftr.FluentClient.VirtualMachineCustomImages
                    .Define(name)
                    .WithRegion(imgOptions.Location)
                    .WithExistingResourceGroup(imgOptions.ResourceGroupName)
                    .WithLinuxFromVhd(vhdUrl, OperatingSystemStateTypes.Generalized)
                    .WithTags(imgOptions.Tags)
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
