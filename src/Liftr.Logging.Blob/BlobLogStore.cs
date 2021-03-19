//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Liftr.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Logging.Blob
{
    public class BlobLogStore : ILogStore
    {
        private readonly BlobContainerClient _logBlobContainer;
        private readonly ITimeSource _timeSource;
        private readonly Serilog.ILogger _logger;

        public BlobLogStore(BlobContainerClient logBlobContainer, ITimeSource timeSource, Serilog.ILogger logger)
        {
            _logBlobContainer = logBlobContainer ?? throw new ArgumentNullException(nameof(logBlobContainer));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logBlobContainer.CreateIfNotExists();
        }

        public async Task<Uri> UploadLogAsync(string logContent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(logContent))
            {
                throw new ArgumentNullException(nameof(logContent));
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(logContent);
            using MemoryStream stream = new MemoryStream(byteArray);
            return await UploadLogAsync(stream, cancellationToken);
        }

        public async Task<Uri> UploadLogAsync(Stream logContent, CancellationToken cancellationToken = default)
        {
            if (logContent == null)
            {
                throw new ArgumentNullException(nameof(logContent));
            }

            try
            {
                var blob = _logBlobContainer.GetBlobClient(GetBlobName());
                await blob.UploadAsync(logContent, cancellationToken);
                return blob.Uri;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "error_upload_log_to_blob. " + ex.Message);
                return null;
            }
        }

        private string GetBlobName()
        {
            var timeStamp = _timeSource.UtcNow;
            var blobName = $"{timeStamp.ToString("yyyy-MM", CultureInfo.InvariantCulture)}/{timeStamp.ToString("dd", CultureInfo.InvariantCulture)}/{timeStamp.ToZuluString()}.txt";
            return blobName.Replace(":", "_"); // avoid ":" being refused by Windows file name restriction
        }
    }
}
