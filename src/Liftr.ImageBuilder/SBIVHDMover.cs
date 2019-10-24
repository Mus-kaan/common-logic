//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Logging;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public class SBIVHDMover
    {
        private readonly Serilog.ILogger _logger;

        public SBIVHDMover(Serilog.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

#pragma warning disable CA2234 // Pass system uri objects instead of strings
#pragma warning disable CA1054 // Uri parameters should not be strings
        public async Task<IEnumerable<Tuple<string, string>>> CopyRegistryInfoAsync(string sbiRegistryInfoDownloadLink, SBIMoverOptions moverOptions, CloudStorageAccount targetStorageAcctount)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            if (moverOptions == null)
            {
                throw new ArgumentNullException(nameof(moverOptions));
            }

            if (moverOptions.Versions == null || !moverOptions.Versions.Any())
            {
                var errMsg = "Please ensure at least one SBI version to copy.";
                _logger.Error(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            if (targetStorageAcctount == null)
            {
                throw new ArgumentNullException(nameof(targetStorageAcctount));
            }

            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            string sbiRegistryContent = null;

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(sbiRegistryInfoDownloadLink);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Cannot download the registry.json file from SBI storage account. Error response: {@SBIResponse}", response);
                    throw new InvalidOperationException("Failed of getting  registry.json file from SBI storage account.");
                }

                sbiRegistryContent = await response.Content.ReadAsStringAsync();
            }

            var sbiRegistries = JsonConvert.DeserializeObject<Dictionary<string, SBIVersionInfo>>(sbiRegistryContent);

            CloudBlobClient blobClient = targetStorageAcctount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(moverOptions.SBIContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            foreach (var version in moverOptions.Versions)
            {
                if (sbiRegistries.TryGetValue(version, out var versionInfo))
                {
                    if (versionInfo.VHDS.TryGetValue(moverOptions.Region, out var vhdUrl))
                    {
                        var srcUri = new Uri(vhdUrl);
                        var targetBlob = blobContainer.GetPageBlobReference(version + ".vhd");
                        var exist = await targetBlob.ExistsAsync();
                        if (exist)
                        {
                            _logger.Information("The SBI VHD is already copied. Skip. Blob Uri: {@blobUrl}", targetBlob.Uri);
                            result.Add(new Tuple<string, string>(version, targetBlob.Uri.AbsoluteUri));
                            continue;
                        }

                        using (var operation = _logger.StartTimedOperation("CopySBIVhdBlobToOurBlobStorage"))
                        {
                            var srcBlob = new CloudPageBlob(srcUri);
                            var copyId = await targetBlob.StartCopyAsync(srcBlob);
                            _logger.Information("Start copying job with {copyId} of sbi VHD with Version {SBIVersion} in region {SBIRegion} to target blob: {@targetSBIBlob}", copyId, version, moverOptions.Region, targetBlob.Uri);
                            await WaitAfterPendingStateAsync(targetBlob);

                            result.Add(new Tuple<string, string>(version, targetBlob.Uri.AbsoluteUri));
                        }
                    }
                    else
                    {
                        var errMsg = $"Cannot find the VHD download link of {moverOptions.Region} of version {version} in the SBI regirsty information.";
                        _logger.Error(errMsg);
                        throw new InvalidOperationException(errMsg);
                    }
                }
                else
                {
                    var errMsg = $"Cannot find verion {version} in the SBI regirsty information.";
                    _logger.Error(errMsg);
                    throw new InvalidOperationException(errMsg);
                }
            }

            return result;
        }
#pragma warning restore CA2234 // Pass system uri objects instead of strings

        private async Task WaitAfterPendingStateAsync(CloudPageBlob blob)
        {
            await blob.FetchAttributesAsync();
            while (blob.CopyState.Status == CopyStatus.Pending)
            {
                _logger.Information("Waiting for blob to finish copy. {@blobUrl}", blob.Uri);
                await Task.Delay(TimeSpan.FromSeconds(10));
                await blob.FetchAttributesAsync();
            }
        }
    }
}
