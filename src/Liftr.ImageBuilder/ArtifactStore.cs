//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Liftr.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ArtifactStore
    {
        private readonly string _storageAccountName;
        private readonly UserDelegationKey _blobDelegationKey;
        private readonly BlobContainerClient _blobContainer;
        private readonly ArtifactStoreOptions _storeOptions;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;

        public ArtifactStore(
            string storageAccountName,
            UserDelegationKey blobDelegationKey,
            BlobContainerClient blobContainer,
            ArtifactStoreOptions storeOptions,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _storageAccountName = storageAccountName;
            _blobDelegationKey = blobDelegationKey ?? throw new ArgumentNullException(nameof(blobDelegationKey));
            _blobContainer = blobContainer ?? throw new ArgumentNullException(nameof(blobContainer));
            _storeOptions = storeOptions ?? throw new ArgumentNullException(nameof(storeOptions));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Uri> UploadBuildArtifactsAndGenerateReadSASAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var errMsg = $"Cannot find the file located at path: {filePath}";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            var fileName = Path.GetFileName(filePath);
            var blob = _blobContainer.GetBlobClient(GetBlobName(fileName));

            _logger.Information("Start uploading local file at path '{filePath}' to cloud blob {@blobUri}", filePath, blob.Uri);
            await blob.UploadAsync(filePath);

            _logger.Information("Generating read only SAS token to cloud blob {@blobUri}", blob.Uri);
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-10),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(_storeOptions.SASTTLInMinutes),
            };

            // Create a SAS token that's valid a short interval.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                Protocol = SasProtocol.Https,
                BlobContainerName = _blobContainer.Name,
                BlobName = blob.Name,
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-10),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_storeOptions.SASTTLInMinutes),
            };
            sasBuilder.SetPermissions(BlobAccountSasPermissions.Read);

            // Use the key to get the SAS token.
            string sasToken = sasBuilder.ToSasQueryParameters(_blobDelegationKey, _storageAccountName).ToString();

            // Construct the full URI, including the SAS token.
            UriBuilder fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = $"{_storageAccountName}.blob.core.windows.net",
                Path = $"{_blobContainer.Name}/{blob.Name}",
                Query = sasToken,
            };

            return fullUri.Uri;
        }

        public async Task<int> CleanUpOldArtifactsAsync()
        {
            int deletedCount = 0;

            // TODO: update CDPx to VS2019 and switch to async api.
            var dateFolders = _blobContainer.GetBlobsByHierarchy(prefix: "drop/", delimiter: "/").ToList();

            foreach (var dateFolder in dateFolders)
            {
                if (dateFolder.IsBlob)
                {
                    continue;
                }

                var timeStamp = DateTime.Parse(dateFolder.Prefix.Split('/')[1], CultureInfo.InvariantCulture);
                var cutOffTime = _timeSource.UtcNow.AddDays(-1 * _storeOptions.OldArtifactTTLInDays);
                if (timeStamp >= cutOffTime)
                {
                    continue;
                }

                var toDeletes = _blobContainer.GetBlobsByHierarchy(prefix: dateFolder.Prefix).ToList();

                foreach (var toDelete in toDeletes)
                {
                    if (!toDelete.IsBlob)
                    {
                        continue;
                    }

                    await _blobContainer.DeleteBlobAsync(toDelete.Blob.Name);
                    _logger.Information("Deleted blob with name {blobName}", toDelete.Blob.Name);
                    deletedCount++;
                }
            }

            return deletedCount;
        }

        internal string GetBlobName(string fileName)
        {
            var timeStamp = _timeSource.UtcNow;
            return $"drop/{timeStamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{timeStamp.ToZuluString()}/{fileName}";
        }

        private string GetBlobName(IListBlobItem item)
        {
            var localPath = item.Uri.LocalPath;
            var name = localPath.Substring(_storeOptions.ContainerName.Length + 2);
            return name;
        }
    }
}
