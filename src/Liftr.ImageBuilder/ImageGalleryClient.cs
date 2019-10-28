//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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

        public async Task<IEnumerable<IGalleryImageVersion>> ListImageVersionsAsync(IAzure fluentClient, string rgName, string sigName, string imageName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Listing image versions. rgName:{rgName}, sigName: {sigName}, imageName: {imageName}", rgName, sigName, imageName);
            var imgs = (await fluentClient.GalleryImageVersions
                .ListByGalleryImageAsync(rgName, sigName, imageName)).ToList();
            _logger.Information("Found {versionCount} image verions. rgName:{rgName}, sigName: {sigName}, imageName: {imageName}", imgs.Count, rgName, sigName, imageName);

            return imgs;
        }

        public async Task DeleteImageVersionAsync(IAzure fluentClient, string rgName, string galleryName, string galleryImageName, string galleryImageVersionName)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            _logger.Information("Deleting image version. rgName:{rgName}, sigName: {sigName}, imageName: {imageName}, galleryImageVersionName: {galleryImageVersionName}", rgName, galleryName, galleryImageName, galleryImageVersionName);
            await fluentClient.GalleryImageVersions
                .DeleteByGalleryImageAsync(rgName, galleryName, galleryImageName, galleryImageVersionName);
            _logger.Information("Finished deleting image version. rgName:{rgName}, sigName: {sigName}, imageName: {imageName}, galleryImageVersionName: {galleryImageVersionName}", rgName, galleryName, galleryImageName, galleryImageVersionName);
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

        public async Task CreateVMImageBuilderTemplateAsync(ILiftrAzure client, Region location, string rgName, string template)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _logger.Information("Start creating AIB template in RG {rgName} ...", rgName);
            try
            {
                var deployment = await client.CreateDeploymentAsync(location, rgName, template);
                _logger.Information("Created AIB template in RG {rgName}. Deployment CorrelationId: {DeploymentCorrelationId}", rgName, deployment.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed at creating AIB template. ");
                throw;
            }
        }

        public async Task<string> RunVMImageBuilderTemplateAsync(ILiftrAzure client, string rgName, string templateName, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/imagebuilder.json#L328
            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/examples/RunImageTemplate.json
            using (var operation = _logger.StartTimedOperation(nameof(RunVMImageBuilderTemplateAsync)))
            using (var handler = new AzureApiAuthHandler(client.AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                _logger.Information("Run AIB template. rgName: {rgName}. templateName: {templateName}", rgName, templateName);
                var uriBuilder = new UriBuilder("https://management.azure.com");
                uriBuilder.Path =
                    $"/subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}/run";
                uriBuilder.Query = "api-version=2019-05-01-preview";
                var startRunResponse = await httpClient.PostAsync(uriBuilder.Uri, null);

                _logger.Information("Start Run response: {@startRunResponse}", startRunResponse);

                if (startRunResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    var asyncOperationResponse = await WaitAsyncOperationAsync(httpClient, startRunResponse, cancellationToken);
                    if (!asyncOperationResponse.OrdinalContains("Succeeded"))
                    {
                        operation.FailOperation(asyncOperationResponse);
                        _logger.Error("Failed at running AIB template. asyncOperationResponse: {asyncOperationResponse}", asyncOperationResponse);
                        throw new InvalidOperationException(asyncOperationResponse);
                    }

                    return asyncOperationResponse;
                }

                var resBody = await startRunResponse.Content.ReadAsStringAsync();
                operation.FailOperation(resBody);
                _logger.Error("Failed at running AIB template. startRunResponse: {startRunResponse}", resBody);
                throw new InvalidOperationException(resBody);
            }
        }

        public async Task<HttpResponseMessage> DeleteVMImageBuilderTemplateAsync(ILiftrAzure client, string rgName, string templateName, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/imagebuilder.json#L280
            using (var handler = new AzureApiAuthHandler(client.AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                _logger.Information("Delete AIB template. rgName: {rgName}. templateName: {templateName}", rgName, templateName);
                var uriBuilder = new UriBuilder("https://management.azure.com");
                uriBuilder.Path =
                    $"/subscriptions/{client.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}";
                uriBuilder.Query = "api-version=2019-05-01-preview";
                var deleteResponse = await httpClient.DeleteAsync(uriBuilder.Uri);

                _logger.Information("Delete AIB template response: {@deleteResponse}", deleteResponse);

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    _logger.Error("Delete AIB template {templateName} in {rgName} failed", templateName, rgName);
                    throw new InvalidOperationException("Delete template failed.");
                }

                _logger.Information("Delete AIB template succeeded. rgName: {rgName}. templateName: {templateName}", rgName, templateName);
                return deleteResponse;
            }
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

        private async Task<string> WaitAsyncOperationAsync(HttpClient client, HttpResponseMessage startOperationResponse, CancellationToken cancellationToken)
        {
            string statusUrl = string.Empty;

            if (startOperationResponse.Headers.Contains("Location"))
            {
                statusUrl = startOperationResponse.Headers.GetValues("Location").FirstOrDefault();
            }

            while (true)
            {
                var statusResponse = await client.GetAsync(new Uri(statusUrl), cancellationToken);
                var body = await statusResponse.Content.ReadAsStringAsync();
                if (body.OrdinalContains("Running") || body.OrdinalContains("InProgress"))
                {
                    _logger.Debug("Waiting for ARM Async Operation. statusUrl: {statusUrl}", statusUrl);
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                }
                else
                {
                    return body;
                }
            }
        }
    }
}
