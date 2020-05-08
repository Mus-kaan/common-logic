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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Microsoft.Liftr.ImageBuilder.Tests")]

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageBuilderOrchestrator
    {
        internal const string c_SBISASSecretName = "SBISASToken";
        private const string c_SBIVersionTag = "SourceSBIVersion";

        private readonly Serilog.ILogger _logger;
        private readonly BuilderOptions _options;
        private readonly LiftrAzureFactory _azFactory;
        private readonly KeyVaultClient _kvClient;
        private readonly ITimeSource _timeSource;
        private ContentStore _contentStore;
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

        public async Task<(IVault, IIdentity)> CreateOrUpdateLiftrImageBuilderInfrastructureAsync(IDictionary<string, string> tags)
        {
            using var ops = _logger.StartTimedOperation(nameof(CreateOrUpdateLiftrImageBuilderInfrastructureAsync));
            try
            {
                var liftrAzure = _azFactory.GenerateLiftrAzure();

                var rg = await liftrAzure.GetOrCreateResourceGroupAsync(_options.Location, _options.ResourceGroupName, tags);
                await liftrAzure.GrantBlobContributorAsync(rg, liftrAzure.SPNObjectId);

                await InitializeAsync();

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

                ImageGalleryClient galleryClient = new ImageGalleryClient(_logger);
                var gallery = await galleryClient.CreateGalleryAsync(liftrAzure.FluentClient, _options.Location, _options.ResourceGroupName, _options.ImageGalleryName, tags);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }

            return (_keyVault, _msi);
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

            AzureImageBuilderTemplateHelper templateHelper = new AzureImageBuilderTemplateHelper(_options, _timeSource);
            await InitializeAsync();

            var galleryClient = new ImageGalleryClient(_logger);
            var aibClient = new AIBClient(_azFactory.GenerateLiftrAzure(), _logger);

            IGalleryImageVersion linuxSourceImage = null;
            PlatformImageIdentifier windowsSourceImage = null;

            if (sourceImageType == SourceImageType.U1604LTS ||
                sourceImageType == SourceImageType.U1804LTS)
            {
                string sbiSASToken = null;
                using (var ops = _logger.StartTimedOperation("RetrieveAzureLinuxSBISasKey"))
                using (var kvValet = new KeyVaultConcierge(_keyVault.VaultUri, _kvClient, _logger))
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

                linuxSourceImage = await CheckLatestSourceSBIAndCacheLocallyAsync(sbiSASToken, sourceImageType, galleryClient);
            }
            else
            {
                windowsSourceImage = SourceImageResolver.ResolveWindowsSourceImage(sourceImageType);
            }

            var artifactUrlWithSAS = await _contentStore.UploadBuildArtifactsToSupportingStorageAsync(artifactPath);
            _logger.Information("uploaded the file {filePath} and generated the url with the SAS token.", artifactPath);

            var templateName = $"{imageName}-{imageVersion}";
            tags[NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString();
            tags["VersionTag"] = imageVersion;

            bool isLinux = true;
            string generatedTemplate;
            if (windowsSourceImage != null)
            {
                generatedTemplate = templateHelper.GenerateWinodwsImageTemplate(
                    _options.Location,
                    templateName,
                    imageName,
                    imageVersion,
                    artifactUrlWithSAS.ToString(),
                    _msi.Id,
                    windowsSourceImage,
                    tags);

                isLinux = false;
            }
            else
            {
                if (linuxSourceImage == null)
                {
                    var ex = new InvalidOperationException($"The source image type '{sourceImageType}' is not supported.");
                    _logger.Fatal(ex, ex.Message);
                    throw ex;
                }

                generatedTemplate = templateHelper.GenerateLinuxImageTemplate(
                    _options.Location,
                    templateName,
                    imageName,
                    imageVersion,
                    artifactUrlWithSAS.ToString(),
                    _msi.Id,
                    linuxSourceImage.Id,
                    tags);
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
                var cleanUpTask = CleanUpAsync(_azFactory.GenerateLiftrAzure().FluentClient, imageName, galleryClient);

                var res = await aibClient.CreateNewSBIVersionByRunAzureVMImageBuilderAsync(
                    _options.Location,
                    _options.ResourceGroupName,
                    templateName,
                    generatedTemplate,
                    cancellationToken);

                _logger.Information("Run AIB template result: {AIBTemplateRunResult}", res);

                await cleanUpTask;
            }

            Uri vhdUri = null;
            if (_options.ExportVHDToStorage)
            {
                var generatedImageSAS = await aibClient.GetGeneratedVDHSASAsync(_options.ResourceGroupName, templateName);
                vhdUri = await _contentStore.CopyGeneratedVHDAsync(generatedImageSAS, imageName, imageVersion);
            }

            if (!_options.KeepAzureVMImageBuilderLogs)
            {
                _logger.Information("Clean up Azure VM Image Builder logs by deleting ther AIB template.");
                await aibClient.DeleteVMImageBuilderTemplateAsync(_options.ResourceGroupName, templateName, cancellationToken);
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

            _logger.Information("The new image version can be found at Shared Image Gallery Image version resource Id: {sigVerionId}", sigImgVersion.Id);
            if (_options.ExportVHDToStorage)
            {
                _logger.Information("The generated image VHD can also be found in the storage account at URL: {vhdUri}", vhdUri);
                _logger.Information("To import the VHDs to another cloud, you can manually generate a SAS token url for the exporting VHD container: {exportingVHDUri}", _contentStore.ExportingVHDContainerUri.ToString());
            }
        }

        #region Private
        private async Task CleanUpAsync(IAzure az, string imageName, ImageGalleryClient galleryClient)
        {
            using var ops = _logger.StartTimedOperation(nameof(CleanUpAsync));

            var deletedArtifactCount = await _contentStore.CleanUpOldArtifactsAsync();
            _logger.Information("Removed old artifacts count: {deletedArtifactCount}", deletedArtifactCount);

            var deletedVHDCount = await _contentStore.CleanUpExportingVHDsAsync();
            _logger.Information("Removed old exporting VHDs: {deletedVHDCount}", deletedVHDCount);

            _logger.Information("Start cleaning up old image versions ...");
            var existingVersions = await galleryClient.ListImageVersionsAsync(az, _options.ResourceGroupName, _options.ImageGalleryName, imageName);
            bool deletedAny = false;
            foreach (var version in existingVersions)
            {
                try
                {
                    var timeStamp = DateTime.Parse(version.Tags[NamingContext.c_createdAtTagName], CultureInfo.InvariantCulture);
                    var cutOffTime = _timeSource.UtcNow.AddDays(-1 * _options.ImageVersionRetentionTimeInDays);
                    if (timeStamp < cutOffTime)
                    {
                        deletedAny = true;
                        _logger.Information("Remove old verion with '{versionId}' since it is older than the cutOffTime: {cutOffTime}", version.Id, cutOffTime);
                        await galleryClient.DeleteImageVersionAsync(az, _options.ResourceGroupName, _options.ImageGalleryName, imageName, version.Name);
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

        private async Task<ContentStore> GetOrCreateArtifactStoreAsync()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>()
            {
                ["FirstCreatedAt"] = _timeSource.UtcNow.ToZuluString(),
            };

            var liftrAzure = _azFactory.GenerateLiftrAzure();

            var existingStorageAccounts = await liftrAzure.ListStorageAccountAsync(_options.ResourceGroupName);
            if (existingStorageAccounts == null || !existingStorageAccounts.Any())
            {
                var storageAccountName = SdkContext.RandomResourceName("liftrimg", 24);
                await liftrAzure.GetOrCreateStorageAccountAsync(_options.Location, _options.ResourceGroupName, storageAccountName, tags);
                existingStorageAccounts = await liftrAzure.ListStorageAccountAsync(_options.ResourceGroupName);
            }

            var storageAccount = existingStorageAccounts.FirstOrDefault();

            if (storageAccount == null)
            {
                var ex = new InvalidOperationException("Cannot find any storage accounts in resource group for storing Image Builder artifacts: " + _options.ResourceGroupName);
                _logger.Error(ex, ex.Message);
                throw ex;
            }

            var blobUri = new Uri($"https://{storageAccount.Name}.blob.core.windows.net");
            BlobServiceClient blobClient = new BlobServiceClient(blobUri, _azFactory.TokenCredential);
            var store = new ContentStore(
                        blobClient,
                        _options.ContentStoreOptions,
                        _timeSource,
                        _logger);

            return store;
        }

        private async Task InitializeAsync()
        {
            if (_contentStore == null)
            {
                var store = await GetOrCreateArtifactStoreAsync();
                Interlocked.Exchange(ref _contentStore, store);
            }

            if (_keyVault == null)
            {
                var kv = await GetOrCreateKeyVaultAsync();
                Interlocked.Exchange(ref _keyVault, kv);
            }

            if (_msi == null)
            {
                var msi = await GetOrCreateMSIAsync();
                Interlocked.Exchange(ref _msi, msi);
            }
        }
        #endregion

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

        #region SBI mover. TODO: delete this after SBI support AME tenant.
        private async Task<IVault> GetOrCreateKeyVaultAsync()
        {
            Dictionary<string, string> tags = new Dictionary<string, string>()
            {
                [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
            };

            var liftrAzure = _azFactory.GenerateLiftrAzure();

            var existingKeyVaults = await liftrAzure.ListKeyVaultAsync(_options.ResourceGroupName);
            if (existingKeyVaults == null || !existingKeyVaults.Any())
            {
                var kvName = SdkContext.RandomResourceName("liftr-img-", 18);
                await liftrAzure.GetOrCreateKeyVaultAsync(_options.Location, _options.ResourceGroupName, kvName, tags);
                existingKeyVaults = await liftrAzure.ListKeyVaultAsync(_options.ResourceGroupName);
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
                        throw new InvalidOperationException("Failed of getting  registry.json file from SBI storage account.");
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

                var localVhdUri = await _contentStore.CopySourceSBIAsync(latestVersion, vhdSASToken);
                var customImage = await CreateCustomImageAsync(latestVersion, localVhdUri.AbsoluteUri);

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

        private async Task<IVirtualMachineCustomImage> CreateCustomImageAsync(string name, string vhdUrl)
        {
            var azureLiftr = _azFactory.GenerateLiftrAzure();

            var customImage = await azureLiftr.FluentClient.VirtualMachineCustomImages
                .GetByResourceGroupAsync(_options.ResourceGroupName, name);

            if (customImage == null)
            {
                _logger.Information("Start creating custom image {name} from vhd Url: {vhdUrl}", name, vhdUrl);
                customImage = await azureLiftr.FluentClient.VirtualMachineCustomImages
                    .Define(name)
                    .WithRegion(_options.Location)
                    .WithExistingResourceGroup(_options.ResourceGroupName)
                    .WithLinuxFromVhd(vhdUrl, OperatingSystemStateTypes.Generalized)
                    .CreateAsync();
                _logger.Information("Created a custom image with resource Id: {customImageId}", customImage.Id);
            }
            else
            {
                _logger.Information("Use the exisitng custom image with resource Id: {customImageId}", customImage.Id);
            }

            return customImage;
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
        #endregion
    }
}
