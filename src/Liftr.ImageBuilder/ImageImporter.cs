//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.KeyVault;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageImporter
    {
        public const string c_exportingStorageAccountConnectionStringSecretName = "ExportStorConnectionString";

        private readonly BuilderOptions _options;
        private readonly IContentStore _artifactStore;
        private readonly LiftrAzureFactory _azFactory;
        private readonly KeyVaultConcierge _kvValet;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;

        public ImageImporter(
            BuilderOptions options,
            IContentStore artifactStore,
            LiftrAzureFactory azFactory,
            KeyVaultConcierge kvValet,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
            _azFactory = azFactory ?? throw new ArgumentNullException(nameof(azFactory));
            _kvValet = kvValet ?? throw new ArgumentNullException(nameof(kvValet));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IGalleryImageVersion> ImportImageVHDAsync(
            string imageName,
            string imageVersion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            if (string.IsNullOrEmpty(imageVersion))
            {
                throw new ArgumentNullException(nameof(imageVersion));
            }

            var az = _azFactory.GenerateLiftrAzure();
            var galleryClient = new ImageGalleryClient(_timeSource, _logger);

            var imgVersion = await galleryClient.GetImageVersionAsync(
                az.FluentClient,
                _options.ResourceGroupName,
                _options.ImageGalleryName,
                imageName,
                imageVersion);

            if (imgVersion != null)
            {
                _logger.Information("There exist same verion in the shared image gallery, stop import and use the existing one: " + imgVersion.Id);
                return imgVersion;
            }

            using var ops = _logger.StartTimedOperation(nameof(ImportImageVHDAsync));
            ops.SetContextProperty(nameof(imageName), imageName);
            ops.SetContextProperty(nameof(imageVersion), imageVersion);
            ops.SetProperty(nameof(_options.ResourceGroupName), _options.ResourceGroupName);
            ops.SetProperty(nameof(_options.ImageGalleryName), _options.ImageGalleryName);
            ops.SetProperty(nameof(_options.SubscriptionId), _options.SubscriptionId.ToString());
            ops.SetProperty(nameof(_options.ImageReplicationRegions), string.Join(", ", _options.ImageReplicationRegions.Select(r => r.Name)));

            _logger.Information($"Start importing VHD image from blob. imageName: {imageName}, imageVersion: {imageVersion}.");

            try
            {
                _logger.Information("Retriving the exporting storage account's connection string from key vault.");
                string storConnectionString;
                try
                {
                    storConnectionString = (await _kvValet.GetSecretAsync(c_exportingStorageAccountConnectionStringSecretName)).Value;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Cannot find the source exporting storage account connection string in the key vault '{_kvValet.VaultUri}' with secret name '{c_exportingStorageAccountConnectionStringSecretName}'. Please set up the Key Vault according to this documentation: https://aka.ms/liftr/import-img";
                    _logger.Fatal(ex, errorMsg);
                    ops.FailOperation(errorMsg);
                    throw new InvalidOperationException(errorMsg, ex);
                }

                BlobServiceClient srcExportStorageAccount = new BlobServiceClient(storConnectionString);
                var srcExportStore = new ContentStore(
                        srcExportStorageAccount,
                        new ContentStoreOptions(),
                        _timeSource,
                        _logger);

                (var vhdSAS, var metaSAS) = await srcExportStore.GetExportedVHDSASTokenAsync(imageName, imageVersion, storConnectionString);

                (var copiedVHD, var meta) = await _artifactStore.CopyVHDToImportAsync(vhdSAS, metaSAS, imageName, imageVersion);
                var tags = meta.GenerateTags();

                bool isLinux = !meta.SourceImageType.IsWindows();
                var customVMImgName = $"{imageName}-{imageVersion}";
                var customImage = await galleryClient.CreateCustomImageFromVHDAsync(az.FluentClient, _options.Location, _options.ResourceGroupName, customVMImgName, copiedVHD, isLinux, tags);

                var targetRegions = _options.ImageReplicationRegions.Select(r => new TargetRegion(r.Name, _options.RegionalReplicaCount)).ToList();

                Dictionary<string, string> imgDefTags = new Dictionary<string, string>()
                {
                    [NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString(),
                    ["Importer" + NamingContext.c_versionTagName] = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,
                };

                tags["Importer" + NamingContext.c_versionTagName] = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

                await galleryClient.CreateImageDefinitionAsync(
                   az.FluentClient,
                   _options.Location,
                   _options.ResourceGroupName,
                   _options.ImageGalleryName,
                   imageName,
                   imgDefTags,
                   isLinux);

                imgVersion = await galleryClient.CreateImageVersionFromCustomImageAsync(
                            az.FluentClient,
                            _options.Location.ToString(),
                            _options.ResourceGroupName,
                            _options.ImageGalleryName,
                            imageName,
                            imageVersion,
                            customImage,
                            tags,
                            targetRegions);

                try
                {
                    _logger.Information("Deleting the intermediate VM custom image: " + customImage.Id);
                    var forget = az.FluentClient.VirtualMachineCustomImages.DeleteByIdAsync(customImage.Id);
                    await CleanUpAsync(imageName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Clean up failed.");
                    throw;
                }

                return imgVersion;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task CleanUpAsync(string imageName)
        {
            using var ops = _logger.StartTimedOperation(nameof(CleanUpAsync));

            try
            {
                var deletedVHDCount = await _artifactStore.CleanUpExportingVHDsAsync();
                _logger.Information("Removed old importing VHDs: {deletedVHDCount}", deletedVHDCount);

                if (_options.ImageVersionRetentionTimeInDays == 0)
                {
                    _logger.Information("Skip clean up old image versions.");
                    return;
                }

                var az = _azFactory.GenerateLiftrAzure();
                var galleryClient = new ImageGalleryClient(_timeSource, _logger);
                await galleryClient.CleanUpOldImageVersionAsync(az.FluentClient, _options.ResourceGroupName, _options.ImageGalleryName, imageName, _options.ImageVersionRetentionTimeInDays);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }
    }
}
