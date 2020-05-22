//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Liftr.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class ContentStore : IContentStore
    {
        private readonly BlobServiceClient _blobClient;
        private readonly BlobContainerClient _artifactBlobContainer;
        private readonly BlobContainerClient _exportingVHDBlobContainer;
        private readonly BlobContainerClient _importingVHDBlobContainer;
        private readonly ContentStoreOptions _storeOptions;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;

        public ContentStore(
            BlobServiceClient blobClient,
            ContentStoreOptions storeOptions,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
            _storeOptions = storeOptions ?? throw new ArgumentNullException(nameof(storeOptions));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            storeOptions.ValidateValues();

            if (blobClient == null)
            {
                throw new ArgumentNullException(nameof(blobClient));
            }

            _artifactBlobContainer = blobClient.GetBlobContainerClient(storeOptions.ArtifactContainerName);

            _exportingVHDBlobContainer = blobClient.GetBlobContainerClient(storeOptions.VHDExportContainerName);

            _importingVHDBlobContainer = blobClient.GetBlobContainerClient(storeOptions.VHDImportContainerName);
        }

        public Uri ExportingVHDContainerUri => _exportingVHDBlobContainer.Uri;

        #region Artifacts management
        public async Task<Uri> UploadBuildArtifactsToSupportingStorageAsync(string filePath)
        {
            using var ops = _logger.StartTimedOperation(nameof(UploadBuildArtifactsToSupportingStorageAsync));
            try
            {
                await _artifactBlobContainer.CreateIfNotExistsAsync();

                if (!File.Exists(filePath))
                {
                    var errMsg = $"Cannot find the file located at path: {filePath}";
                    _logger.Error(errMsg);
                    throw new InvalidOperationException(errMsg);
                }

                var fileName = Path.GetFileName(filePath);
                var blob = _artifactBlobContainer.GetBlobClient(GetBlobName(fileName));

                _logger.Information("Start uploading local file at path '{filePath}' to cloud blob {@blobUri}", filePath, blob.Uri);
                await blob.UploadAsync(filePath);

                _logger.Information("Generating read only SAS token to cloud blob {@blobUri}", blob.Uri);

                // Create a SAS token that's valid a short interval.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    Protocol = SasProtocol.Https,
                    Resource = "b", // b is for blobs
                    BlobContainerName = _artifactBlobContainer.Name,
                    BlobName = blob.Name,
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-10),
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_storeOptions.SASTTLInMinutes),
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Use the key to get the SAS token.
                var delegationKey = (await _blobClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(_storeOptions.SASTTLInMinutes))).Value;
                string sasToken = sasBuilder.ToSasQueryParameters(delegationKey, _blobClient.AccountName).ToString();

                // Construct the full URI, including the SAS token.
                UriBuilder fullUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = _blobClient.Uri.Host,
                    Path = $"{_artifactBlobContainer.Name}/{blob.Name}",
                    Query = sasToken,
                };

                return fullUri.Uri;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task DeleteBuildArtifactAsync(Uri blobSas)
        {
            if (blobSas == null)
            {
                throw new ArgumentNullException(nameof(blobSas));
            }

            var target = new BlobClient(blobSas);
            var blob = _artifactBlobContainer.GetBlobClient(target.Name);
            await blob.DeleteIfExistsAsync();
        }

        public async Task<int> CleanUpOldArtifactsAsync()
        {
            int deletedCount = 0;

            if (!(await _artifactBlobContainer.ExistsAsync()))
            {
                return deletedCount;
            }

            var dateFolders = await ToListAsync(_artifactBlobContainer.GetBlobsByHierarchyAsync(prefix: "drop/", delimiter: "/"));

            foreach (var dateFolder in dateFolders)
            {
                if (dateFolder.IsBlob)
                {
                    continue;
                }

                var timeStamp = DateTime.Parse(dateFolder.Prefix.Split('/')[1], CultureInfo.InvariantCulture);
                var cutOffTime = _timeSource.UtcNow.AddDays(-1 * _storeOptions.ContentTTLInDays);
                if (timeStamp >= cutOffTime)
                {
                    continue;
                }

                var toDeletes = await ToListAsync(_artifactBlobContainer.GetBlobsByHierarchyAsync(prefix: dateFolder.Prefix));

                foreach (var toDelete in toDeletes)
                {
                    if (!toDelete.IsBlob)
                    {
                        continue;
                    }

                    await _artifactBlobContainer.DeleteBlobAsync(toDelete.Blob.Name);
                    _logger.Information("Deleted blob with name {blobName}", toDelete.Blob.Name);
                    deletedCount++;
                }
            }

            return deletedCount;
        }
        #endregion

        public async Task<Uri> GetExportingContainerSASTokenAsync()
        {
            // Use the key to get the SAS token.
            var delegationKey = (await _blobClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(_storeOptions.ExportingVHDContainerSASTTLInDays))).Value;

            // Create a SAS token that's valid a short interval.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                Protocol = SasProtocol.Https,
                Resource = "c", // c is for containers
                BlobContainerName = _exportingVHDBlobContainer.Name,
                BlobName = string.Empty,
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-10),
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(_storeOptions.ExportingVHDContainerSASTTLInDays),
            };
            sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List);

            string sasToken = sasBuilder.ToSasQueryParameters(delegationKey, _blobClient.AccountName).ToString();

            // Construct the full URI, including the SAS token.
            UriBuilder fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = _blobClient.Uri.Host,
                Path = $"{_exportingVHDBlobContainer.Name}",
                Query = sasToken,
            };

            return fullUri.Uri;
        }

        public async Task<(Uri, Uri)> GetExportedVHDSASTokenAsync(string imageName, string imageVersion, string connectionString)
        {
            using var ops = _logger.StartTimedOperation(nameof(GetExportedVHDSASTokenAsync));

            // Final structure is like:
            // /exporting-vhds/TestImageName/1.2.32/TestImageName-1.2.32.vhd
            // /exporting-vhds/TestImageName/1.2.32/TestImageName-1.2.32-metadata.json
            try
            {
                AzureStorageConnectionString azStr = null;
                if (!AzureStorageConnectionString.TryParseConnectionString(connectionString, out azStr))
                {
                    var ex = new InvalidOperationException("The storage connection string is in invalid format.");
                    _logger.Fatal(ex.Message);
                    throw ex;
                }

                var vhdSAS = await GetExportingBlobSASTokenAsync(VHDBlobName(imageName, imageVersion), azStr);
                var metaSAS = await GetExportingBlobSASTokenAsync(VHDMetaBlobName(imageName, imageVersion), azStr);

                return (vhdSAS, metaSAS);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task<BlobClient> CopyBlobAsync(
           Uri sourceSASToken,
           string targetContainerName,
           string targetBlobName,
           string operationName = nameof(CopyBlobAsync),
           CancellationToken cancellationToken = default)
        {
            var blobContainer = _blobClient.GetBlobContainerClient(targetContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            var targetBlob = blobContainer.GetBlobClient(targetBlobName);
            var exist = await targetBlob.ExistsAsync();
            if (exist)
            {
                _logger.Information("The target blob already exists. Skip copying. Blob Uri: {blobUrl}", targetBlob.Uri.AbsoluteUri);
                return targetBlob;
            }

            using (_logger.StartTimedOperation(operationName))
            {
                _logger.Information("Start copying to blob with URI: {blobUri}", targetBlob.Uri.AbsoluteUri);
                var operation = await targetBlob.StartCopyFromUriAsync(sourceSASToken, cancellationToken: cancellationToken);
                await operation.WaitForCompletionAsync(cancellationToken);
            }

            return targetBlob;
        }

        public async Task<Uri> CopySourceSBIAsync(string sbiVhdVersion, Uri sourceVHDSASToken)
        {
            var targetBlobName = sbiVhdVersion + ".vhd";
            _logger.Information("Checking and move the Azure Linux base SBI VHD with version '{sbiVhdVerion}' to local storage account for caching. This will improve the image generation proces in the next build.", sbiVhdVersion);
            var blob = await CopyBlobAsync(sourceVHDSASToken, _storeOptions.SourceSBIContainerName, targetBlobName, operationName: "CopyAzLinuxSBIVHDToCachedLocalBlob");
            return blob.Uri;
        }

        public async Task<Uri> CopyVHDToExportAsync(
            Uri sourceVHDSASToken,
            string imageName,
            string imageVersion,
            SourceImageType sourceImageType,
            IReadOnlyDictionary<string, string> tags,
            string createdAt,
            string deleteAfter)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            using var ops = _logger.StartTimedOperation(nameof(CopyVHDToExportAsync));

            // Final structure is like:
            // /exporting-vhds/TestImageName/1.2.32/TestImageName-1.2.32.vhd
            // /exporting-vhds/TestImageName/1.2.32/TestImageName-1.2.32-metadata.json
            try
            {
                var targetBlobName = VHDBlobName(imageName, imageVersion);
                _logger.Information("Moving the AIB generated VHD to exporting blob: {targetBlobName}", targetBlobName);
                var blob = await CopyBlobAsync(sourceVHDSASToken, _storeOptions.VHDExportContainerName, targetBlobName, operationName: "CopyGeneratedVHD");
                var blobPropeties = await blob.GetPropertiesAsync();

                var meta = new VHDMeta()
                {
                    ImageName = imageName,
                    ImageVersion = imageVersion,
                    CreatedAtUTC = createdAt,
                    DeleteAfterUTC = deleteAfter,
                    CopiedAtUTC = _timeSource.UtcNow.Date.ToZuluString(),
                    SourceImageType = sourceImageType,
                };

                if (blobPropeties?.Value?.ContentHash != null &&
                    blobPropeties.Value.ContentHash.Length > 0)
                {
                    meta.ContentHash = Convert.ToBase64String(blobPropeties.Value.ContentHash);
                }
                else
                {
                    _logger.Warning("The content hash in the blob properties is null.");
                    if (blobPropeties?.Value != null)
                    {
                        _logger.Warning("Blob properties: {@blobProperties}.", blobPropeties.Value);
                    }
                }

                meta.OtherTags = tags.ToJson();

                var metaBlobName = VHDMetaBlobName(imageName, imageVersion);
                var metaBlob = _exportingVHDBlobContainer.GetBlobClient(metaBlobName);
                if (!(await metaBlob.ExistsAsync()))
                {
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(meta.ToJson(indented: true))))
                    {
                        await metaBlob.UploadAsync(ms);
                    }
                }

                return blob.Uri;
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public async Task<(Uri, VHDMeta)> CopyVHDToImportAsync(Uri sourceVHDSASToken, Uri sourceVHDMetaSASToken, string imageName, string imageVersion)
        {
            using var ops = _logger.StartTimedOperation(nameof(CopyVHDToImportAsync));

            // Final structure is like:
            // /importing-vhds/TestImageName/1.2.32/TestImageName-1.2.32.vhd
            // /importing-vhds/TestImageName/1.2.32/TestImageName-1.2.32-metadata.json
            try
            {
                VHDMeta meta;
                var metaBlob = await CopyBlobAsync(sourceVHDMetaSASToken, _storeOptions.VHDImportContainerName, VHDMetaBlobName(imageName, imageVersion), operationName: nameof(CopyVHDToImportAsync) + "MetaImpl");
                var downloadResponse = await metaBlob.DownloadAsync();
                using (StreamReader reader = new StreamReader(downloadResponse.Value.Content, Encoding.UTF8))
                {
                    var metaContent = reader.ReadToEnd();
                    meta = metaContent.FromJson<VHDMeta>();
                }

                var targetBlobName = VHDBlobName(imageName, imageVersion);
                _logger.Information("Moving the VHD to importing blob: {targetBlobName}. This can easily take 15 minutes, e.g. it took 14.2 minutes to move a 30GB VHD from west US to China North.", targetBlobName);
                var vhdBlob = await CopyBlobAsync(sourceVHDSASToken, _storeOptions.VHDImportContainerName, targetBlobName, operationName: nameof(CopyVHDToImportAsync) + "Impl");

                return (vhdBlob.Uri, meta);
            }
            catch (Exception ex)
            {
                ops.FailOperation(ex.Message);
                throw;
            }
        }

        public Task<int> CleanUpExportingVHDsAsync()
        {
            return CleanUpVHDsAsync(_exportingVHDBlobContainer);
        }

        public Task<int> CleanUpImportingVHDsAsync()
        {
            return CleanUpVHDsAsync(_importingVHDBlobContainer);
        }

        private async Task<int> CleanUpVHDsAsync(BlobContainerClient container)
        {
            // Final structure is like:
            // /exporting-vhds/TestImageName/1.2.32/TestImageName-1.2.32.vhd
            // /exporting-vhds/TestImageName/1.2.32/TestImageName-1.2.32-metadata.json
            int deletedCount = 0;
            if (!(await container.ExistsAsync()))
            {
                return deletedCount;
            }

            var imageFolders = await ToListAsync(container.GetBlobsByHierarchyAsync(delimiter: "/"));

            var cutOffTime = _timeSource.UtcNow.AddDays(-1 * _storeOptions.ContentTTLInDays);

            foreach (var imageFolder in imageFolders)
            {
                if (imageFolder.IsBlob)
                {
                    continue;
                }

                var imageName = imageFolder.Prefix.Split('/')[0];
                var imageVersionFolders = await ToListAsync(container.GetBlobsByHierarchyAsync(prefix: imageFolder.Prefix, delimiter: "/"));

                foreach (var imageVersionFolder in imageVersionFolders)
                {
                    if (imageFolder.IsBlob)
                    {
                        continue;
                    }

                    var imageVersion = imageVersionFolder.Prefix.Split('/')[1];
                    var metaBlob = container.GetBlobClient(VHDMetaBlobName(imageName, imageVersion));
                    var exist = await metaBlob.ExistsAsync();
                    if (!exist)
                    {
                        continue;
                    }

                    VHDMeta imgMeta = null;
                    try
                    {
                        var downloadResponse = await metaBlob.DownloadAsync();
                        using (StreamReader reader = new StreamReader(downloadResponse.Value.Content, Encoding.UTF8))
                        {
                            var metaContent = reader.ReadToEnd();
                            imgMeta = metaContent.FromJson<VHDMeta>();
                        }

                        var copiedAt = DateTime.Parse(imgMeta.CopiedAtUTC, CultureInfo.InvariantCulture);
                        if (copiedAt >= cutOffTime)
                        {
                            continue;
                        }

                        var vhdBlob = container.GetBlobClient(VHDBlobName(imageName, imageVersion));
                        await vhdBlob.DeleteIfExistsAsync();
                        await metaBlob.DeleteIfExistsAsync();
                        deletedCount++;
                    }
                    catch
                    {
                    }
                }
            }

            return deletedCount;
        }

        private async Task<Uri> GetExportingBlobSASTokenAsync(string blobName, AzureStorageConnectionString connectionString)
        {
            if (!(await _exportingVHDBlobContainer.ExistsAsync()))
            {
                var ex = new InvalidOperationException($"Cannot find the container with name '{_exportingVHDBlobContainer.Name}' in the storage account '{_blobClient.Uri}'. See more information at: https://aka.ms/liftr/import-img");
                _logger.Fatal(ex.Message);
                throw ex;
            }

            var blob = _exportingVHDBlobContainer.GetBlobClient(blobName);
            var exist = await blob.ExistsAsync();
            if (!exist)
            {
                var ex = new InvalidOperationException($"Cannot find the exporting blob at '{blob.Uri}'. See more information at: https://aka.ms/liftr/import-img");
                _logger.Fatal(ex.Message);
                throw ex;
            }

            // Create a SAS token that's valid a short interval.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                Protocol = SasProtocol.Https,
                Resource = "b", // b is for blobs
                BlobContainerName = blob.BlobContainerName,
                BlobName = blob.Name,
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-10),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(5),
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            StorageSharedKeyCredential cred = new StorageSharedKeyCredential(connectionString.AccountName, connectionString.AccountKey);
            string sasToken = sasBuilder.ToSasQueryParameters(cred).ToString();

            // Construct the full URI, including the SAS token.
            UriBuilder fullUri = new UriBuilder(blob.Uri);
            fullUri.Query = sasToken;

            return fullUri.Uri;
        }

        private static async Task<List<T>> ToListAsync<T>(AsyncPageable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            List<T> results = new List<T>();

            await foreach (var page in source)
            {
                results.Add(page);
            }

            return results;
        }

        private static Task<List<BlobHierarchyItem>> ToListAsync(AsyncPageable<BlobHierarchyItem> source, CancellationToken cancellationToken = default)
        {
            return ToListAsync<BlobHierarchyItem>(source);
        }

        private string GetBlobName(string fileName)
        {
            var timeStamp = _timeSource.UtcNow;
            return $"drop/{timeStamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{timeStamp.ToZuluString()}/{fileName}";
        }

        private static string VHDBlobName(string imageName, string imageVersion)
        {
            return $"{imageName}/{imageVersion}/{imageName}-{imageVersion}.vhd";
        }

        private static string VHDMetaBlobName(string imageName, string imageVersion)
        {
            return $"{imageName}/{imageVersion}/{imageName}-{imageVersion}-metadata.json";
        }

        private string GetBlobName(IListBlobItem item)
        {
            var localPath = item.Uri.LocalPath;
            var name = localPath.Substring(_storeOptions.ArtifactContainerName.Length + 2);
            return name;
        }
    }
}
