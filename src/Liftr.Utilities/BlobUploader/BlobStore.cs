//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Utilities.BlobUploader
{
    public static class BlobStore
    {
        /// <summary>
        /// Compress the given directory, upload to Azure blob storage for given connectionString and container
        /// </summary>
        /// <param name="blobServiceClient">blob client to upload</param>
        /// <param name="containerName">blob container</param>
        /// <param name="sourceDirectory">Directory to compress and upload</param>
        /// <param name="sasUriTimeOffSetInMinutes">Time in minutes until which the shareable uri is valid</param>
        /// <param name="logger">logger</param>
        /// <returns>Azure storage blob url</returns>
        public static async Task<Uri> CompressAndStoreAsync(BlobServiceClient blobServiceClient, string containerName, string sourceDirectory, DateTimeOffset sasUriTimeOffSetInMinutes, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            ZipCreator.CompressDirectory(sourceDirectory, tempFilePath, logger);

            logger.Information($"[{BlobUploaderConstants.BlobUploadTag}] File compressed successfully at {tempFilePath}, starting upload");
            return await StoreAsync(blobServiceClient, containerName, tempFilePath, sasUriTimeOffSetInMinutes, logger);
        }

        /// <summary>
        /// Store the given file as a blob in the container and reutrn the AbsoluteUri of blob
        /// </summary>
        /// <param name="blobServiceClient">blob client to upload</param>
        /// <param name="containerName">blob container</param>
        /// <param name="filePath"></param>
        /// <param name="sasUriTimeOffSetInMinutes">Time in minutes until which the shareable uri is valid</param>
        /// <param name="logger">logger</param>
        /// <returns>Uploaded blob url</returns>
        /// <exception cref="Azure.RequestFailedException">Azure blob storage uplaod failed</exception>
        public static async Task<Uri> StoreAsync(BlobServiceClient blobServiceClient, string containerName, string filePath, DateTimeOffset sasUriTimeOffSetInMinutes, ILogger logger)
        {
            if (blobServiceClient == null)
            {
                throw new ArgumentNullException(nameof(blobServiceClient));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            containerName = !string.IsNullOrWhiteSpace(containerName) ? containerName : throw new ArgumentNullException(nameof(containerName));

            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobName = Guid.NewGuid().ToString();
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(filePath);

            if (!blobClient.Exists())
            {
                var ex = new Azure.RequestFailedException("Upload failed, please try again");
                logger.Fatal(ex, $"[{BlobUploaderConstants.BlobUploadTag}] Upload failed, please try again");
                throw ex;
            }

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, sasUriTimeOffSetInMinutes);
            logger.Information($"[{BlobUploaderConstants.BlobUploadTag}] Generated SasUri");
            return sasUri;
        }

        /// <summary>
        /// Store the given file as a blob in the container and reutrn the AbsoluteUri of blob
        /// </summary>
        /// <param name="blobServiceClient">blob client to upload</param>
        /// <param name="containerName">blob container</param>
        /// <param name="filePath"></param>
        /// <param name="logger">logger</param>
        /// <returns>Uploaded blob url</returns>
        /// <exception cref="Azure.RequestFailedException">Azure blob storage uplaod failed</exception>
        public static async Task<Uri> StoreAsync(BlobServiceClient blobServiceClient, string containerName, string filePath, ILogger logger)
        {
            return await StoreAsync(blobServiceClient, containerName, filePath, DateTimeOffset.MaxValue, logger);
        }
    }
}
