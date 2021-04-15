//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Liftr.Blob;
using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.StatusStore.Blob
{
    public class BlobStatusReader : IStatusReader
    {
        public const string HistoryBlobPrefix = "history";

        protected readonly BlobContainerClient _blobContainerClient;
        protected readonly ITimeSource _timeSource;
        protected readonly Serilog.ILogger _logger;

        public BlobStatusReader(
            BlobContainerClient blobContainerClient,
            ITimeSource timeSource,
            Serilog.ILogger logger)
        {
            _blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IList<IStatusRecord>> GetStateAsync(string key, string machineName = null)
        {
            var prefix = machineName == null ? $"{key}/" : $"{key}/{machineName}";
            return ListRecordsAsync(prefix);
        }

        public Task<IList<IStatusRecord>> GetHistoryAsync(string key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = "all";
            }

            return ListRecordsAsync($"{HistoryBlobPrefix}/{key}", isJsonL: true);
        }

        protected async Task<IList<IStatusRecord>> ListRecordsAsync(string prefix, bool isJsonL = false)
        {
            List<IStatusRecord> records = new List<IStatusRecord>();
            var statusBlobs = await _blobContainerClient.ListBlobsByHierarchyAsync(prefix: prefix);

            foreach (var statusBlob in statusBlobs)
            {
                if (!statusBlob.IsBlob)
                {
                    continue;
                }

                var blobClient = _blobContainerClient.GetBlobClient(statusBlob.Blob.Name);

                string text;
                using (var memoryStream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(memoryStream);
                    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                }

                if (!isJsonL)
                {
                    records.Add(ParseStatusRecord(text));
                }
                else
                {
                    var lines = text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    records.AddRange(lines.Select(l => ParseStatusRecord(l)));
                }
            }

            return records.OrderBy(r => r.TimeStamp).ToList();
        }

        protected static StatusRecord ParseStatusRecord(string input)
        {
            var record = input.FromJson<StatusRecord>();
            record.Value = record.Value.FromBase64();
            return record;
        }

        protected static string GetStatusBlobName(string prefix, string machineName)
        {
            return $"{prefix}/{machineName}.json";
        }

        protected string GetHistoryBlobName(string prefix)
        {
            var timeStamp = _timeSource.UtcNow;
            return $"{prefix}/{timeStamp.ToString("yyyy-MM", CultureInfo.InvariantCulture)}/{timeStamp.ToString("dd", CultureInfo.InvariantCulture)}.txt";
        }
    }
}
