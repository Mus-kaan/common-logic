//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ImageGalleryClient
    {
        private readonly Serilog.ILogger _logger;

        public ImageGalleryClient(Serilog.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Gallery operations
        public async Task<IGallery> CreateGalleryAsync(
            IAzure fluentClient,
            Region location,
            string rgName,
            string galleryName,
            IDictionary<string, string> tags)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            var gallery = await GetGalleryAsync(fluentClient, rgName, galleryName);
            if (gallery != null)
            {
                _logger.Information("Using existing Shared Image Gallery with Id {resourceId} ...", gallery.Id);
                return gallery;
            }

            gallery = await fluentClient.Galleries
                .Define(galleryName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created Shared Image Gallery with resourceId {resourceId}", gallery.Id);

            return gallery;
        }

        public async Task<IGallery> GetGalleryAsync(
            IAzure fluentClient,
            string rgName,
            string galleryName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Getting Shared Image Gallery with name {galleryName} in RG {rgName} ...", galleryName, rgName);
            var gallery = await fluentClient.Galleries
                .GetByResourceGroupAsync(rgName, galleryName);

            return gallery;
        }
        #endregion

        #region Image Definition operations
        public async Task<IGalleryImage> CreateImageDefinitionAsync(
            IAzure fluentClient,
            Region location,
            string rgName,
            string galleryName,
            string imageName,
            IDictionary<string, string> tags,
            bool isLinux = true)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            var image = await GetImageDefinitionAsync(fluentClient, rgName, galleryName, imageName);
            if (image != null)
            {
                _logger.Information("Using existing Gallery Image Definition with Id {resourceId} ...", image.Id);
                return image;
            }

            if (isLinux)
            {
                _logger.Information("Creating a Gallery Image Definition for Linux with name {imageName} ...", imageName);
                image = await fluentClient.GalleryImages
                    .Define(imageName)
                    .WithExistingGallery(rgName, galleryName)
                    .WithLocation(location)
                    .WithIdentifier(publisher: "AzureLiftr", offer: "UbuntuSecureBaseImage", sku: imageName)
                    .WithGeneralizedLinux()
                    .WithTags(tags)
                    .CreateAsync();
            }
            else
            {
                _logger.Information("Creating a Gallery Image Definition for Windows with name {imageName} ...", imageName);
                image = await fluentClient.GalleryImages
                    .Define(imageName)
                    .WithExistingGallery(rgName, galleryName)
                    .WithLocation(location)
                    .WithIdentifier(publisher: "AzureLiftr", offer: "WindowsServerBaseImage", sku: imageName)
                    .WithGeneralizedWindows()
                    .WithTags(tags)
                    .CreateAsync();
            }

            _logger.Information("Created a Gallery image with resourceId {resourceId}", image.Id);

            return image;
        }

        public async Task<IGalleryImage> GetImageDefinitionAsync(
            IAzure fluentClient,
            string rgName,
            string galleryName,
            string imageName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Getting Gallery image Definition with image name {imageName} in gallery {galleryName} in RG {rgName} ...", imageName, galleryName, rgName);
            try
            {
                var img = await fluentClient.GalleryImages
                    .GetByGalleryAsync(rgName, galleryName, imageName);
                return img;
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                return null;
            }
        }
        #endregion

        #region Image Version operations
        public async Task<IGalleryImageVersion> GetImageVersionAsync(
            IAzure fluentClient,
            string rgName,
            string galleryName,
            string imageName,
            string imageVersionName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            if (imageName == null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            _logger.Information("Getting image verion. imageVersionName:{imageVersionName}, galleryName: {galleryName}, imageName: {imageName}", imageVersionName, galleryName, imageName);

            try
            {
                var galleryImageVersion = await fluentClient.GalleryImageVersions
                .GetByGalleryImageAsync(rgName, galleryName, imageName, imageVersionName);
                return galleryImageVersion;
            }
            catch (CloudException ex) when (ex.IsNotFound())
            {
                return null;
            }
        }

        public async Task<IEnumerable<IGalleryImageVersion>> ListImageVersionsAsync(
            IAzure fluentClient,
            string rgName,
            string galleryName,
            string imageName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Listing image versions. rgName:{rgName}, galleryName: {galleryName}, imageName: {imageName}", rgName, galleryName, imageName);
            var imgs = (await fluentClient.GalleryImageVersions
                .ListByGalleryImageAsync(rgName, galleryName, imageName)).ToList();
            _logger.Information("Found {versionCount} image verions. rgName:{rgName}, galleryName: {galleryName}, imageName: {imageName}", imgs.Count, rgName, galleryName, imageName);

            return imgs;
        }

        public async Task DeleteImageVersionAsync(
            IAzure fluentClient,
            string rgName,
            string galleryName,
            string imageName,
            string imageVersionName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Deleting image version. rgName:{rgName}, galleryName: {galleryName}, imageName: {imageName}, imageVersionName: {imageVersionName}", rgName, galleryName, imageName, imageVersionName);
            await fluentClient.GalleryImageVersions
                .DeleteByGalleryImageAsync(rgName, galleryName, imageName, imageVersionName);
            _logger.Information("Finished deleting image version. rgName:{rgName}, galleryName: {galleryName}, imageName: {imageName}, imageVersionName: {imageVersionName}", rgName, galleryName, imageName, imageVersionName);
        }

        public async Task<IGalleryImageVersion> CreateImageVersionFromCustomImageAsync(
            IAzure fluentClient,
            string location,
            string rgName,
            string galleryName,
            string imageName,
            string imageVersionName,
            IVirtualMachineCustomImage customImage,
            IDictionary<string, string> tags,
            IList<TargetRegion> regions = null)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            if (imageName == null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            var galleryImageVersion = await GetImageVersionAsync(fluentClient, rgName, galleryName, imageName, imageVersionName);

            if (galleryImageVersion != null)
            {
                _logger.Information("Using existing Gallery Image Version with Id {resourceId} ...", galleryImageVersion.Id);
            }
            else
            {
                _logger.Information("Creating a new verion of Shared Gallery Image. imageVersionName:{imageVersionName}, galleryName: {galleryName}, imageName: {imageName}", imageVersionName, galleryName, imageName);

                using (var operation = _logger.StartTimedOperation("CreateNewGalleryImageVersionFromCustomImage"))
                {
                    var galleryImageVersionCratable = fluentClient.GalleryImageVersions
                    .Define(imageVersionName)
                    .WithExistingImage(rgName, galleryName, imageName)
                    .WithLocation(location)
                    .WithSourceCustomImage(customImage)
                    .WithTags(tags);

                    if (regions != null)
                    {
                        galleryImageVersionCratable = galleryImageVersionCratable.WithRegionAvailability(regions);
                    }

                    galleryImageVersion = await galleryImageVersionCratable.CreateAsync();
                }

                _logger.Information("Created Shared Gallery Image Verion with resourceId {resourceId}", galleryImageVersion.Id);
            }

            return galleryImageVersion;
        }
        #endregion
    }
}