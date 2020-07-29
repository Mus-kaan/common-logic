//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using Microsoft.Liftr.SBI.Mover;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.ImageBuilder.Tests")]

namespace Microsoft.Liftr.ImageBuilder
{
    public enum InfrastructureType
    {
        BakeNewImage,
        BakeNewImageAndExport,
        ImportImage,
    }

    public class ImageBuilderOrchestrator
    {
        internal const string c_SBISASSecretName = "SBISASToken";
        private const string c_SBIVersionTag = "SourceSBIVersion";

        private const string c_artifactStorageNamePrefix = "liftrimg";
        private const string c_exportingStorageNamePrefix = "export";

        private const string c_keyVaultNamePrefix = "liftr-img-";

        private const string c_packerFilesFolderName = "packer-files";
        private const string c_entryScriptLinux = "bake-image.sh";

        private readonly Serilog.ILogger _logger;
        private readonly BuilderOptions _options;
        private readonly LiftrAzureFactory _azFactory;
        private readonly KeyVaultClient _kvClient;
        private readonly ITimeSource _timeSource;
        private ContentStore _artifactStore;
        private ContentStore _exportStore;
        private IVault _keyVault;
        private IIdentity _msi;

        public ImageBuilderOrchestrator(
            BuilderOptions options,
            LiftrAzureFactory azFactory,
            KeyVaultClient kvClient,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _azFactory = azFactory ?? throw new ArgumentNullException(nameof(azFactory));
            _kvClient = kvClient ?? throw new ArgumentNullException(nameof(kvClient));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(IVault, IContentStore)> CreateOrUpdateLiftrImageBuilderInfrastructureAsync(InfrastructureType infraType, SourceImageType? sourceImageType, IDictionary<string, string> tags)
        {
            using var ops = _logger.StartTimedOperation(nameof(CreateOrUpdateLiftrImageBuilderInfrastructureAsync));
            try
            {
                var liftrAzure = _azFactory.GenerateLiftrAzure();

                var rg = await liftrAzure.GetOrCreateResourceGroupAsync(_options.Location, _options.ResourceGroupName, tags);
                await liftrAzure.GrantBlobContributorAsync(rg, liftrAzure.SPNObjectId);

                ImageGalleryClient galleryClient = new ImageGalleryClient(_timeSource, _logger);
                var gallery = await galleryClient.CreateGalleryAsync(liftrAzure.FluentClient, _options.Location, _options.ResourceGroupName, _options.ImageGalleryName, tags);

                _keyVault = await GetOrCreateKeyVaultAsync(c_keyVaultNamePrefix);

                _artifactStore = await GetOrCreateContentStoreAsync(c_artifactStorageNamePrefix);

                if (infraType == InfrastructureType.BakeNewImageAndExport)
                {
                    _exportStore = await GetOrCreateContentStoreAsync(c_exportingStorageNamePrefix);
                }

                if (infraType == InfrastructureType.ImportImage)
                {
                    return (_keyVault, _artifactStore);
                }

                _msi = await GetOrCreateMSIAsync();

                try
                {
                    _logger.Information("Granting resource group '{rgId}' contributor role to the Azure VM Image Builder's Managed Identity {msiId} ...", rg.Id, _msi.Id);
                    await liftrAzure.Authenticated.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(_msi.GetObjectId())
                    .WithBuiltInRole(BuiltInRole.Contributor)
                    .WithResourceGroupScope(rg)
                    .CreateAsync();
                }
                catch (CloudException ex) when (ex.IsDuplicatedRoleAssignment())
                {
                }
                catch (CloudException ex) when (ex.Message.OrdinalContains("does not exist in the directory"))
                {
                    _logger.Fatal("You probably selected a wrong tenant. Please correct the tenant selection in the configuration file. We only support MS tenant and AME tenant for now.");
                    throw;
                }

                return (_keyVault, _artifactStore);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task BuildCustomizedSBIAsync(
            string imageName,
            string imageVersion,
            SourceImageType sourceImageType,
            string artifactPath,
            IDictionary<string, string> tags,
            CancellationToken cancellationToken)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            if (Version.TryParse(imageVersion, out var parsedVersion))
            {
                imageVersion = parsedVersion.ToString();
            }
            else
            {
                throw new InvalidImageVersionException($"The image version value '{imageVersion}' is invalid. ");
            }

            _logger.Information("GeneratingImageVersion: {imageVersion}", imageVersion);

            if (!File.Exists(artifactPath))
            {
                var errMsg = $"Cannot find the artifact file located at {artifactPath}";
                _logger.Error(errMsg);
                throw new FileNotFoundException(errMsg);
            }

            bool isZip = artifactPath.OrdinalEndsWith("zip");
            using var kvValet = new KeyVaultConcierge(_keyVault.VaultUri, _kvClient, _logger);
            if (isZip)
            {
                artifactPath = await CheckAndModeifyArtifactZipAsync(
                    artifactPath,
                    kvValet,
                    imageName,
                    imageVersion,
                    sourceImageType);
            }

            AzureImageBuilderTemplateHelper templateHelper = new AzureImageBuilderTemplateHelper(_options, _timeSource);

            var galleryClient = new ImageGalleryClient(_timeSource, _logger);
            var aibClient = new AIBClient(_azFactory.GenerateLiftrAzure(), _logger);

            using var rootOperation = _logger.StartTimedOperation(nameof(BuildCustomizedSBIAsync));
            try
            {
                var artifactUrlWithSAS = await _artifactStore.UploadBuildArtifactsToSupportingStorageAsync(artifactPath);
                _logger.Information("uploaded the file '{filePath}' and generated the url with the SAS token.", artifactPath);
                if (isZip)
                {
                    File.Delete(artifactPath);
                }

                bool isLinux = true;
                string generatedTemplate;
                var templateName = $"{imageName}-{imageVersion}";

                // 0 means do not delete. Put a really long date here.
                int ttlInDays = _options.ImageVersionRetentionTimeInDays == 0 ? 3000 : _options.ImageVersionRetentionTimeInDays;
                var deleteAfterStr = _timeSource.UtcNow.AddDays(ttlInDays).ToZuluString();
                var createdAtStr = _timeSource.UtcNow.ToZuluString();
                tags[NamingContext.c_createdAtTagName] = createdAtStr;
                tags["VersionTag"] = imageVersion;
                tags["LiftrSourceImageType"] = sourceImageType.ToString();

                Dictionary<string, string> imgVersionTags = new Dictionary<string, string>(tags);
                imgVersionTags[ImageGalleryClient.c_deleteAfterTagName] = deleteAfterStr;

                if (!sourceImageType.IsPlatformImage())
                {
                    string sbiSASToken = null;
                    using (var ops = _logger.StartTimedOperation("RetrieveAzureLinuxSBISASKey"))
                    {
                        try
                        {
                            sbiSASToken = (await kvValet.GetSecretAsync(c_SBISASSecretName)).Value;
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"Cannot find the SBI SASKeys in the key vault '{_keyVault.Id}' with secret name '{c_SBISASSecretName}'. Please acquire a SBI SASKeys according to this documentation: https://aka.ms/liftr/sbi-sas";
                            _logger.Fatal(ex, errorMsg);
                            ops.FailOperation(errorMsg);
                            throw;
                        }
                    }

                    var linuxSourceImage = await CheckLatestSourceSBIAndCacheLocallyAsync(sbiSASToken, sourceImageType, galleryClient);
                    generatedTemplate = templateHelper.GenerateLinuxSBITemplate(
                        _options.Location,
                        templateName,
                        imageName,
                        imageVersion,
                        artifactUrlWithSAS.ToString(),
                        _msi.Id,
                        linuxSourceImage.Id,
                        imgVersionTags,
                        isZip);
                }
                else if (sourceImageType.IsWindows())
                {
                    isLinux = false;
                    var windowsSourceImage = SourceImageResolver.ResolvePlatformSourceImage(sourceImageType);
                    generatedTemplate = templateHelper.GenerateWinodwsPlatformImageTemplate(
                        _options.Location,
                        templateName,
                        imageName,
                        imageVersion,
                        artifactUrlWithSAS.ToString(),
                        _msi.Id,
                        windowsSourceImage,
                        imgVersionTags);
                }
                else
                {
                    var linuxPlatformImage = SourceImageResolver.ResolvePlatformSourceImage(sourceImageType);
                    generatedTemplate = templateHelper.GenerateLinuxPlatformImageTemplate(
                        _options.Location,
                        templateName,
                        imageName,
                        imageVersion,
                        artifactUrlWithSAS.ToString(),
                        _msi.Id,
                        linuxPlatformImage,
                        imgVersionTags,
                        isZip);
                }

                var az = _azFactory.GenerateLiftrAzure();
                var imgDefinition = await galleryClient.GetImageDefinitionAsync(
                    az.FluentClient,
                    _options.ResourceGroupName,
                    _options.ImageGalleryName,
                    imageName);

                if (imgDefinition == null)
                {
                    await galleryClient.CreateImageDefinitionAsync(
                    az.FluentClient,
                    _options.Location,
                    _options.ResourceGroupName,
                    _options.ImageGalleryName,
                    imageName,
                    tags,
                    isLinux: isLinux);
                }

                var sigImgVersion = await galleryClient.GetImageVersionAsync(
                    _azFactory.GenerateLiftrAzure().FluentClient,
                    _options.ResourceGroupName,
                    _options.ImageGalleryName,
                    imageName,
                    imageVersion);

                if (sigImgVersion != null)
                {
                    var ex = new DuplicatedSharedImageGalleryImagerVersionException($"There already exist a same image version '{imageVersion}' for image '{imageName}' in gallery '{_options.ImageGalleryName}'. We cannot generate another image version with this name.");
                    _logger.Fatal(ex.Message);
                    throw ex;
                }
                else
                {
                    var existingTemplate = await aibClient.GetAIBTemplateAsync(_options.ResourceGroupName, templateName);
                    if (!string.IsNullOrEmpty(existingTemplate))
                    {
                        var aibResourceId = $"/subscriptions/{az.FluentClient.SubscriptionId}/resourceGroups/{_options.ResourceGroupName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}";
                        var aibRunState = GetLastRunState(existingTemplate);
                        _logger.Information("There exist the same AIB template with Id '{aibResourceId}' in runState '{aibRunState}'", aibResourceId, aibRunState);

                        if (aibRunState.OrdinalEquals("Running"))
                        {
                            throw new InvalidOperationException("There exist another running AIB template with the same resource Id: " + aibResourceId);
                        }

                        await aibClient.DeleteVMImageBuilderTemplateAsync(_options.ResourceGroupName, templateName);
                    }

                    var res = await aibClient.CreateNewSBIVersionByRunAzureVMImageBuilderAsync(
                        _options.Location,
                        _options.ResourceGroupName,
                        templateName,
                        generatedTemplate,
                        cancellationToken);

                    _logger.Information("Run AIB template result: {AIBTemplateRunResult}", res);

                    await CleanUpAsync(_azFactory.GenerateLiftrAzure().FluentClient, imageName, galleryClient);
                }

                sigImgVersion = await galleryClient.GetImageVersionAsync(
                    _azFactory.GenerateLiftrAzure().FluentClient,
                    _options.ResourceGroupName,
                    _options.ImageGalleryName,
                    imageName,
                    imageVersion);

                if (sigImgVersion == null)
                {
                    throw new InvalidOperationException("Cannot find the generated SIG version.");
                }

                Uri vhdUri = null;
                if (_options.ExportVHDToStorage)
                {
                    _logger.Information("Start exporting generted VHD to a storage account.");
                    var generatedImageSAS = await aibClient.GetGeneratedVDHSASAsync(_options.ResourceGroupName, templateName);

                    vhdUri = await _exportStore.CopyVHDToExportAsync(
                        new Uri(generatedImageSAS),
                        imageName,
                        imageVersion,
                        sourceImageType,
                        sigImgVersion.Tags,
                        createdAtStr,
                        deleteAfterStr);
                }

                if (!_options.KeepAzureVMImageBuilderLogs)
                {
                    _logger.Information("Clean up Azure VM Image Builder logs by deleting ther AIB template.");
                    await aibClient.DeleteVMImageBuilderTemplateAsync(_options.ResourceGroupName, templateName, cancellationToken);
                }

                _logger.Information("The new image version can be found at Shared Image Gallery Image version resource Id: {sigVerionId}", sigImgVersion.Id);
                if (_options.ExportVHDToStorage)
                {
                    _logger.Information("The generated image VHD can also be found in the storage account at URL: {vhdUri}", vhdUri);
                    _logger.Information("To export and import the generated VM image in another cloud, see instructions: https://aka.ms/liftr/import-img");
                }

                _logger.Information("Delete the build artifact in storage blob: {artifactUri}", artifactUrlWithSAS.AbsolutePath);
                await _artifactStore.DeleteBuildArtifactAsync(artifactUrlWithSAS);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed at building SBI.");
                rootOperation.FailOperation(ex.Message);
                throw;
            }
        }

        private async Task CleanUpAsync(IAzure az, string imageName, ImageGalleryClient galleryClient)
        {
            if (_options.ImageVersionRetentionTimeInDays == 0)
            {
                _logger.Information("Skip clean up old image versions.");
                return;
            }

            using var ops = _logger.StartTimedOperation(nameof(CleanUpAsync));

            try
            {
                var deletedArtifactCount = await _artifactStore.CleanUpOldArtifactsAsync();
                _logger.Information("Removed old artifacts count: {deletedArtifactCount}", deletedArtifactCount);

                if (_exportStore != null)
                {
                    var deletedVHDCount = await _exportStore.CleanUpExportingVHDsAsync();
                    _logger.Information("Removed old exporting VHDs: {deletedVHDCount}", deletedVHDCount);
                }

                _logger.Information("Start cleaning up old image versions ...");
                await galleryClient.CleanUpOldImageVersionAsync(az, _options.ResourceGroupName, _options.ImageGalleryName, imageName, _options.ImageVersionRetentionTimeInDays);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        private async Task<ContentStore> GetOrCreateContentStoreAsync(string storageNamePrefix)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>()
            {
                [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
            };

            var liftrAzure = _azFactory.GenerateLiftrAzure();

            var existingStorageAccounts = await liftrAzure.ListStorageAccountAsync(_options.ResourceGroupName, storageNamePrefix);
            if (existingStorageAccounts == null || !existingStorageAccounts.Any())
            {
                var storageAccountName = SdkContext.RandomResourceName(storageNamePrefix, 24);
                await liftrAzure.GetOrCreateStorageAccountAsync(_options.Location, _options.ResourceGroupName, storageAccountName, tags);
                existingStorageAccounts = await liftrAzure.ListStorageAccountAsync(_options.ResourceGroupName, storageNamePrefix);
            }

            var storageAccount = existingStorageAccounts.FirstOrDefault();

            if (storageAccount == null)
            {
                var ex = new InvalidOperationException($"Cannot find or create any storage accounts with prefix '{storageNamePrefix}' in resource group: " + _options.ResourceGroupName);
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var blobUri = new Uri(storageAccount.Inner.PrimaryEndpoints.Blob);
            BlobServiceClient blobClient = new BlobServiceClient(blobUri, _azFactory.TokenCredential);
            var store = new ContentStore(
                        blobClient,
                        _options.ContentStoreOptions,
                        _timeSource,
                        _logger);

            return store;
        }

        private async Task<IIdentity> GetOrCreateMSIAsync()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>()
            {
                [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
            };

            var liftrAzure = _azFactory.GenerateLiftrAzure();
            var msiName = $"lib-{_options.ImageGalleryName}-{_options.Location.ShortName()}-mi".ToLowerInvariant();
            var msi = await liftrAzure.GetOrCreateMSIAsync(_options.Location, _options.ResourceGroupName, msiName, tags);

            return msi;
        }

        private async Task<IVault> GetOrCreateKeyVaultAsync(string namePrefix)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>()
            {
                [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
            };

            var liftrAzure = _azFactory.GenerateLiftrAzure();

            var existingKeyVaults = await liftrAzure.ListKeyVaultAsync(_options.ResourceGroupName, namePrefix);
            if (existingKeyVaults == null || !existingKeyVaults.Any())
            {
                var kvName = SdkContext.RandomResourceName(namePrefix, 18);
                await liftrAzure.GetOrCreateKeyVaultAsync(_options.Location, _options.ResourceGroupName, kvName, tags);
                existingKeyVaults = await liftrAzure.ListKeyVaultAsync(_options.ResourceGroupName, namePrefix);
            }

            var kv = existingKeyVaults.FirstOrDefault();

            if (kv == null)
            {
                var ex = new InvalidOperationException("Cannot configure the key vault to store the SBI SAS token in resource group: " + _options.ResourceGroupName);
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            await liftrAzure.GrantSelfKeyVaultAdminAccessAsync(kv);

            return kv;
        }

        private async Task<IGalleryImageVersion> CheckLatestSourceSBIAndCacheLocallyAsync(string sbiSASToken, SourceImageType sourceImageType, ImageGalleryClient galleryClient)
        {
            using var ops = _logger.StartTimedOperation(nameof(CheckLatestSourceSBIAndCacheLocallyAsync));

            try
            {
                string latestVersion = null;
                string vhdSASToken = null;
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(sbiSASToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Error("Cannot download the registry.json file from SBI storage account. Error response: {@SBIResponse}", response);
                        if (response.Content != null)
                        {
                            _logger.Error("Error content:" + await response.Content.ReadAsStringAsync());
                        }

                        throw new InvalidOperationException("Failed of getting registry.json file from SBI storage account.");
                    }

                    var sbiRegistryContent = await response.Content.ReadAsStringAsync();
                    var vhdRegistry = JsonConvert.DeserializeObject<Dictionary<string, SBIVersionInfo>>(sbiRegistryContent);
                    latestVersion = FindLastestSBIVersionTag(vhdRegistry, sourceImageType, _options.Location);
                    _logger.Information("Resolved {region} latest SBI version: {latestVersion}", _options.Location.Name, latestVersion);

                    if (string.IsNullOrEmpty(latestVersion))
                    {
                        var ex = new InvalidOperationException("Cannot list the latest version of the SBI VHDs.");
                        _logger.Fatal(ex, ex.Message);
                        throw ex;
                    }

                    vhdSASToken = vhdRegistry[latestVersion].VHDS.Where(kvp => _options.Location.Name.OrdinalEquals(Region.Create(kvp.Key).Name)).FirstOrDefault().Value;

                    if (string.IsNullOrEmpty(vhdSASToken))
                    {
                        var ex = new InvalidOperationException($"Cannot find the vhd SAS token for version {latestVersion} in region {_options.Location.Name}.");
                        _logger.Fatal(ex, ex.Message);
                        throw ex;
                    }
                }

                var az = _azFactory.GenerateLiftrAzure();
                var existingVersions = await galleryClient.ListImageVersionsAsync(
                    az.FluentClient,
                    _options.ResourceGroupName,
                    _options.ImageGalleryName,
                    sourceImageType.ToString());

                var targetingVersion = existingVersions.Where(ver =>
                {
                    if (ver?.Tags.ContainsKey(c_SBIVersionTag) == true)
                    {
                        return ver.Tags[c_SBIVersionTag].OrdinalEquals(latestVersion);
                    }

                    return false;
                }).FirstOrDefault();

                if (targetingVersion != null)
                {
                    _logger.Information("The lastest SBI VHD is already copied to the local Shared Image Gallery. Use the existing SBI SIG version: {imageVersionId}", targetingVersion.Id);
                    return targetingVersion;
                }

                var tags = new Dictionary<string, string>()
                {
                    [c_SBIVersionTag] = latestVersion,
                    [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
                };

                var localVhdUri = await _artifactStore.CopySourceSBIAsync(latestVersion, new Uri(vhdSASToken));
                var customImage = await galleryClient.CreateCustomImageFromVHDAsync(az.FluentClient, _options.Location, _options.ResourceGroupName, latestVersion, localVhdUri, isLinux: true);

                var sbiImgDefinition = await galleryClient.CreateImageDefinitionAsync(
                    az.FluentClient,
                    _options.Location,
                    _options.ResourceGroupName,
                    _options.ImageGalleryName,
                    sourceImageType.ToString(),
                    tags);

                var targetRegions = _options.ImageReplicationRegions.Select(r => new TargetRegion(r.Name, _options.RegionalReplicaCount)).ToList();

                var imgVersion = await galleryClient.CreateImageVersionFromCustomImageAsync(
                            az.FluentClient,
                            _options.Location.ToString(),
                            _options.ResourceGroupName,
                            _options.ImageGalleryName,
                            sourceImageType.ToString(),
                            GetImageVersion(latestVersion),
                            customImage,
                            tags,
                            targetRegions);

                _logger.Information("Start deleting intermidiate custom image with Id 'customImageId'", customImage.Id);
                var forget = az.FluentClient.VirtualMachineCustomImages.DeleteByIdAsync(customImage.Id);

                return imgVersion;
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, ex.Message);
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        private string FindLastestSBIVersionTag(Dictionary<string, SBIVersionInfo> vhdRegistry, SourceImageType sourceImageType, Region region)
        {
            // sample versions: "U1804LTS_Vb-6", "U1804LTS_Vb-5", "U1804LTS_Mn-1", "U1804LTS_Mn-2", "U1804LTS_latest"
            var versions = vhdRegistry
                    .Select(kvp => kvp.Key)
                    .Where(ver => ver.OrdinalStartsWith(sourceImageType.ToString()));

            _logger.Information("Listed Azure Linux SBI versions for {sourceImageType}: {@sbiVHDVersions}", sourceImageType.ToString(), versions);

            var latestVersion = versions.FirstOrDefault(ver => ver.OrdinalContains("latest"));

            if (string.IsNullOrEmpty(latestVersion))
            {
                var ex = new InvalidOperationException("Cannot list the version of the SBI VHDs with 'latest' tag.");
                _logger.Fatal(ex, ex.Message);
                throw ex;
            }

            var lastestVHDKvp = vhdRegistry[latestVersion].VHDS.FirstOrDefault(vhdKvp => region.Equals(Region.Create(vhdKvp.Key)));

            var sematicVersionObject = vhdRegistry
                .Where(kvp => !kvp.Key.OrdinalEquals(latestVersion))
                .FirstOrDefault(kvp => kvp.Value.VHDS.ContainsKey(lastestVHDKvp.Key) && kvp.Value.VHDS[lastestVHDKvp.Key].OrdinalEquals(lastestVHDKvp.Value));

            return sematicVersionObject.Key;
        }

        private async Task<string> CheckAndModeifyArtifactZipAsync(
            string artifactPath,
            KeyVaultConcierge kvValet,
            string imageName,
            string imageVersion,
            SourceImageType sourceImageType)
        {
            _logger.Information("Checking the file content in '{artifactPath}' ...", artifactPath);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(artifactPath);

            var workingFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingFolder);

            var localZipFile = Path.Combine(workingFolder, $"{fileNameNoExt}-original.zip");
            var localUnzipFolder = Path.Combine(workingFolder, "content");

            File.Copy(artifactPath, localZipFile);

            _logger.Information("Unzip file to path: {folderPath}", localUnzipFolder);
            ZipFile.ExtractToDirectory(localZipFile, localUnzipFolder);

            if (!Directory.Exists(Path.Combine(localUnzipFolder, c_packerFilesFolderName)))
            {
                var ex = new InvalidArtifactPackageException($"Cannot find the '{c_packerFilesFolderName}' folder in '{artifactPath}'");
                _logger.Fatal(ex.Message);
                throw ex;
            }

            if (!File.Exists(Path.Combine(localUnzipFolder, c_packerFilesFolderName, c_entryScriptLinux)))
            {
                var ex = new InvalidArtifactPackageException($"Cannot find the entry script '{c_entryScriptLinux}' under '{c_packerFilesFolderName}' folder in '{artifactPath}'");
                _logger.Fatal(ex.Message);
                throw ex;
            }

            File.WriteAllText(Path.Combine(localUnzipFolder, c_packerFilesFolderName, $"image-name.txt"), imageName);
            File.WriteAllText(Path.Combine(localUnzipFolder, c_packerFilesFolderName, $"image-version.txt"), imageVersion);
            File.WriteAllText(Path.Combine(localUnzipFolder, c_packerFilesFolderName, $"source-image-type.txt"), sourceImageType.ToString());

            _logger.Information($"Downloading supporting secrets from key vault ...");
            int cnt = 0;
            var secretsToCopy = await kvValet.ListSecretsAsync();
            foreach (var secret in secretsToCopy)
            {
                if (secret.Identifier.Name.OrdinalEquals(c_SBISASSecretName))
                {
                    continue;
                }

                var secretBundle = await kvValet.GetSecretAsync(secret.Identifier.Name);
                var secretFilePath = Path.Combine(localUnzipFolder, c_packerFilesFolderName, $"{secret.Identifier.Name}.txt");
                File.WriteAllText(secretFilePath, secretBundle.Value);
                _logger.Information("Downloaded '{secretName}' to file '{seceretFile}'", secret.Identifier.Name, secretFilePath);
                cnt++;
            }

            _logger.Information("Downloaded {copiedSecretCount} secrets from key vault.", cnt);

            var generateZip = Path.Combine(workingFolder, $"{fileNameNoExt}.zip");
            ZipFile.CreateFromDirectory(localUnzipFolder, generateZip);

            File.Delete(localZipFile);
            Directory.Delete(localUnzipFolder, recursive: true);

            return generateZip;
        }

        private static string GetImageVersion(string imageName)
        {
            // sample input: 'U1804LTS_Mn-3'
            int digitVersion = 1;
            foreach (var c in imageName)
            {
                if (char.IsDigit(c))
                {
                    digitVersion = (digitVersion * 10) + (c - '0');
                }
                else if (char.IsLetter(c))
                {
                    digitVersion = (digitVersion * 10) + char.ToLower(c, CultureInfo.InvariantCulture) - 'a';
                }
            }

            Version ver = new Version(0, 1, digitVersion);
            return ver.ToString();
        }

        private static string GetLastRunState(string aibTemplate)
        {
            try
            {
                dynamic templateObj = JObject.Parse(aibTemplate);
                return (string)templateObj.properties.lastRunStatus.runState;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
