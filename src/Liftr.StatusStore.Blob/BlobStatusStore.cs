//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.DiagnosticSource;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.StatusStore.Blob
{
    public class BlobStatusStore : BlobStatusReader, IStatusStore
    {
        private readonly IWriterMetaData _writerMetaData;

        public BlobStatusStore(
            IWriterMetaData writerMetaData,
            BlobContainerClient blobContainerClient,
            ITimeSource timeSource,
            Serilog.ILogger logger)
            : base(blobContainerClient, timeSource, logger)
        {
            _writerMetaData = writerMetaData ?? throw new ArgumentNullException(nameof(writerMetaData));
            if (string.IsNullOrEmpty(writerMetaData.MachineName))
            {
                throw new InvalidOperationException($"'{nameof(writerMetaData.MachineName)}' in '{nameof(WriterMetaData)}' cannot be null.");
            }
        }

        public async Task<Uri> UpdateStateAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            var record = new StatusRecord(_writerMetaData)
            {
                Key = key,
                TimeStamp = _timeSource.UtcNow,
                Value = value.ToBase64(),
            };

            var telemetryContext = TelemetryContext.GetCurrent();
            if (telemetryContext != null && !string.IsNullOrEmpty(telemetryContext?.CorrelationId))
            {
                record.CorrelationId = telemetryContext.CorrelationId;
            }

            var statusBlob = await UpdateStateAsync(key, record, cancellationToken);

            var allHistoryBlobName = GetHistoryBlobName($"{HistoryBlobPrefix}/all");
            var allHistoryBlob = await AppendHistoryAsync(allHistoryBlobName, record, cancellationToken);

            var targetHistoryBlobName = GetHistoryBlobName($"{HistoryBlobPrefix}/{key}");
            var targetHistoryBlob = await AppendHistoryAsync(targetHistoryBlobName, record, cancellationToken);

            return statusBlob.Uri;
        }

        public async Task<IStatusRecord> GetCurrentMachineStateAsync(string key)
        {
            var records = await GetStateAsync(key, _writerMetaData.MachineName);

            if (records == null || !records.Any())
            {
                return null;
            }

            if (records.Count > 1)
            {
                throw new InvalidOperationException($"There are more than 1 states for key '{key}' and machine name '{_writerMetaData.MachineName}'");
            }

            return records.FirstOrDefault();
        }

        private async Task<BlobClient> UpdateStateAsync(string key, StatusRecord record, CancellationToken cancellationToken)
        {
            var statusBlob = _blobContainerClient.GetBlobClient(GetStatusBlobName(key, _writerMetaData.MachineName));

            var recordContent = record.ToJson(indented: true);
            byte[] byteArray = Encoding.UTF8.GetBytes(recordContent);
            using MemoryStream stream = new MemoryStream(byteArray);
            await statusBlob.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

            return statusBlob;
        }

        private async Task<AppendBlobClient> AppendHistoryAsync(string blobName, StatusRecord record, CancellationToken cancellationToken)
        {
            var appendBlob = _blobContainerClient.GetAppendBlobClient(blobName);
            await appendBlob.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var recordContent = record.ToJson() + "\n";
            byte[] byteArray = Encoding.UTF8.GetBytes(recordContent);
            using MemoryStream stream = new MemoryStream(byteArray);
            await appendBlob.AppendBlockAsync(stream, cancellationToken: cancellationToken);

            return appendBlob;
        }
    }
}
