//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Liftr.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

        public async Task<string> UploadBuildArtifactsAndGenerateReadSASAsync(CloudStorageAccount storageAccount, string filePath)
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

        public async Task<int> CleanUpOldArtifactsAsync(CloudStorageAccount storageAccount)
        {
            int deletedCount = 0;

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_storeOptions.ContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            var directory = blobContainer.GetDirectoryReference("drop");
            var folders = directory.ListBlobs().Where(b => b as CloudBlobDirectory != null).ToList();
            foreach (var folder in folders)
            {
                var timeStamp = DateTime.Parse(folder.Uri.LocalPath.Split('/')[3], CultureInfo.InvariantCulture);
                var cutOffTime = _timeSource.UtcNow.AddDays(-1 * _storeOptions.OldArtifactTTLInDays);
                if (timeStamp < cutOffTime)
                {
                    var toDeletes = blobContainer.ListBlobs(prefix: GetBlobName(folder), useFlatBlobListing: true);

                    foreach (var toDelete in toDeletes)
                    {
                        var blob = blobContainer.GetBlockBlobReference(GetBlobName(toDelete));
                        await blob.DeleteAsync();
                        _logger.Information("Deleted blob with name {blobUri}", toDelete.Uri.AbsoluteUri);
                        deletedCount++;
                    }
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
