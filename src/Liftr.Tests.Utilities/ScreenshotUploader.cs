//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Liftr.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Tests.Utilities
{
    public class ScreenshotUploader
    {
        private readonly BlobContainerClient _container;
        private readonly ITimeSource _timeSource;

        public ScreenshotUploader(string connectionString, string blobContainerName = "screenshot", ITimeSource timeSource = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _timeSource = timeSource ?? new SystemTimeSource();

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            _container = blobServiceClient.GetBlobContainerClient(blobContainerName);
        }

        public async Task<Uri> UploadScreenshotAsync(string screenshotPath)
        {
            await _container.CreateIfNotExistsAsync();
            var fileName = Path.GetFileName(screenshotPath);
            var blob = _container.GetBlobClient(GetBlobName(fileName));
            await blob.UploadAsync(screenshotPath);
            return blob.Uri;
        }

        private string GetBlobName(string fileName)
        {
            var timeStamp = _timeSource.UtcNow;
            return $"{timeStamp.ToString("yyyy-MM", CultureInfo.InvariantCulture)}/{timeStamp.ToString("dd", CultureInfo.InvariantCulture)}/{timeStamp.ToZuluString()}/{fileName}";
        }
    }
}
