//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public async Task<IGalleryImageVersion> CreateImageVersionAsync(IAzure fluentClient, string location, string rgName, string sigName, string imageName, IVirtualMachineCustomImage customImage, IDictionary<string, string> tags)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            if (imageName == null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            var imgVersionName = GetImageVersion(imageName);

            var galleryImageVersion = await GetImageVersionAsync(fluentClient, rgName, sigName, imageName);

            if (galleryImageVersion != null)
            {
                _logger.Information("Using existing Gallery Image Version with Id {resourceId} ...", galleryImageVersion.Id);
            }
            else
            {
                _logger.Information("Creating a new verion of Shared Gallery Image. imgVersionName:{imgVersionName}, sigName: {sigName}, imageName: {imageName}", imgVersionName, sigName, imageName);

                using (var operation = _logger.StartTimedOperation("CreateNewGalleryImageVersionFromCustomImage"))
                {
                    galleryImageVersion = await fluentClient.GalleryImageVersions
                    .Define(imgVersionName)
                    .WithExistingImage(rgName, sigName, imageName)
                    .WithLocation(location)
                    .WithSourceCustomImage(customImage)
                    .WithTags(tags)
                    .CreateAsync();
                }

                _logger.Information("Created Shared Gallery Image Verion with resourceId {resourceId}", galleryImageVersion.Id);
            }

            return galleryImageVersion;
        }

        public async Task<IGalleryImageVersion> GetImageVersionAsync(IAzure fluentClient, string rgName, string sigName, string imageName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            if (imageName == null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            var imgVersionName = GetImageVersion(imageName);

            _logger.Information("Getting image verion. imgVersionName:{imgVersionName}, sigName: {sigName}, imageName: {imageName}", imgVersionName, sigName, imageName);

            try
            {
                var galleryImageVersion = await fluentClient.GalleryImageVersions
                .GetByGalleryImageAsync(rgName, sigName, imageName, imgVersionName);
                return galleryImageVersion;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Warning(ex, "Cannot get image verion.");
                return null;
            }
        }

        public async Task<IGalleryImage> CreateImageDefinitionAsync(IAzure fluentClient, Region location, string rgName, string sigName, string imageName, IDictionary<string, string> tags)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            var image = await GetImageDefinitionAsync(fluentClient, rgName, sigName, imageName);
            if (image != null)
            {
                _logger.Information("Using existing Gallery Image Definition with Id {resourceId} ...", image.Id);
                return image;
            }

            _logger.Information("Creating a Gallery Image Definition with name {imageName} ...", imageName);
            image = await fluentClient.GalleryImages
                .Define(imageName)
                .WithExistingGallery(rgName, sigName)
                .WithLocation(location)
                .WithIdentifier(publisher: "AzureLiftr", offer: "UbuntuSecureBaseImage", sku: imageName)
                .WithGeneralizedLinux()
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created a Gallery image with resourceId {resourceId}", image.Id);

            return image;
        }

        public async Task<IGalleryImage> GetImageDefinitionAsync(IAzure fluentClient, string rgName, string sigName, string imageName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Getting Gallery image Definition with image name {imageName} in gallery {sigName} in RG {rgName} ...", imageName, sigName, rgName);
            try
            {
                var img = await fluentClient.GalleryImages
                    .GetByGalleryAsync(rgName, sigName, imageName);
                return img;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Warning(ex, "Cannot get image Definition.");
                return null;
            }
        }

        public async Task<IGallery> CreateGalleryAsync(IAzure fluentClient, Region location, string rgName, string sigName, IDictionary<string, string> tags)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Creating a Shared Image Gallery with name {sigName} ...", sigName);

            var gallery = await GetGalleryAsync(fluentClient, rgName, sigName);
            if (gallery != null)
            {
                _logger.Information("Using existing Shared Image Gallery with Id {resourceId} ...", gallery.Id);
                return gallery;
            }

            gallery = await fluentClient.Galleries
                .Define(sigName)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithTags(tags)
                .CreateAsync();

            _logger.Information("Created Shared Image Gallery with resourceId {resourceId}", gallery.Id);

            return gallery;
        }

        public async Task<IGallery> GetGalleryAsync(IAzure fluentClient, string rgName, string sigName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Getting Shared Image Gallery with name {sigName} in RG {rgName} ...", sigName, rgName);
            var gallery = await fluentClient.Galleries
                .GetByResourceGroupAsync(rgName, sigName);

            return gallery;
        }

        private static string GetImageVersion(string imageName)
        {
            string digitPart = "1";
            foreach (var c in imageName)
            {
                if (char.IsDigit(c))
                {
                    digitPart += c;
                }
            }

            int buildVersion = int.Parse(digitPart, CultureInfo.InvariantCulture);
            Version ver = new Version(0, 1, buildVersion);
            return ver.ToString();
        }
    }
}
