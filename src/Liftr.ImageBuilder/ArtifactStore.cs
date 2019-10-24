//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Liftr.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ArtifactStore
    {
        private readonly ArtifactStoreOptions _storeOptions;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;

        public ArtifactStore(ArtifactStoreOptions storeOptions, ITimeSource timeSource, Serilog.ILogger logger)
        {
            _storeOptions = storeOptions ?? throw new ArgumentNullException(nameof(storeOptions));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> UploadFileAndGenerateReadSASAsync(CloudStorageAccount storageAccount, string filePath)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException(nameof(storageAccount));
            }

            if (!File.Exists(filePath))
            {
                var errMsg = $"Cannot find the file located at path: {filePath}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_storeOptions.ContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            var fileName = Path.GetFileName(filePath);
            var blob = blobContainer.GetBlockBlobReference(GetBlobName(fileName));

            _logger.Information("Start uploading local file at path '{filePath}' to cloud blob {@blobUri}", filePath, blob.Uri);
            await blob.UploadFromFileAsync(filePath);

            _logger.Information("Generating read only SAS token to cloud blob {@blobUri}", blob.Uri);
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-10),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(_storeOptions.SASTTLInMinutes),
            };
            var sas = blob.GetSharedAccessSignature(policy);

            return blob.Uri.ToString() + sas;
        }

        internal string GetBlobName(string fileName)
        {
            var timeStamp = _timeSource.UtcNow;
            return $"{timeStamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{timeStamp.ToZuluString()}/{fileName}";
        }
    }
}
