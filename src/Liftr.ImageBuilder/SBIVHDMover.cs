//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class SBIVHDMover
    {
        private const string c_sbiMoverContainerName = "sbi-source-images";

        private readonly Serilog.ILogger _logger;

        public SBIVHDMover(Serilog.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "<Pending>")]
        public async Task<Uri> CopyRegistryInfoAsync(
            string sbiVhdVerion,
            string sourceVHDSASToken,
            CloudStorageAccount targetStorageAcctount)
        {
            if (targetStorageAcctount == null)
            {
                throw new ArgumentNullException(nameof(targetStorageAcctount));
            }

            List<Tuple<string, string>> result = new List<Tuple<string, string>>();

            CloudBlobClient blobClient = targetStorageAcctount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(c_sbiMoverContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            var srcUri = new Uri(sourceVHDSASToken);
            var targetBlob = blobContainer.GetPageBlobReference(sbiVhdVerion + ".vhd");
            var exist = await targetBlob.ExistsAsync();
            if (exist)
            {
                _logger.Information("The SBI VHD is already copied. Skip. Blob Uri: {@blobUrl}", targetBlob.Uri);
                return targetBlob.Uri;
            }

            using (var operation = _logger.StartTimedOperation("CopySBIVhdBlobToLocalBlobStorage"))
            {
                var srcBlob = new CloudPageBlob(srcUri);
                var copyId = await targetBlob.StartCopyAsync(srcBlob);
                _logger.Information("Start copying job with {copyId} of sbi VHD with Version {SBIVersion} to target blob: {@targetSBIBlob}", copyId, sbiVhdVerion, targetBlob.Uri);
                await WaitAfterPendingStateAsync(targetBlob);
            }

            return targetBlob.Uri;
        }
#pragma warning restore CA2234 // Pass system uri objects instead of strings

        private async Task WaitAfterPendingStateAsync(CloudPageBlob blob)
        {
            await blob.FetchAttributesAsync();
            while (blob.CopyState.Status == CopyStatus.Pending)
            {
                _logger.Debug("Waiting for blob to finish copy. {@blobUrl}", blob.Uri);
                await Task.Delay(TimeSpan.FromSeconds(60));
                await blob.FetchAttributesAsync();
            }
        }
    }
}
