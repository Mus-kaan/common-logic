//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ThanosAssetRegistryManager
    {
        public const string BlobContainerName = "thanos-asset";
        public const string BlobName = "thanos-asset-registry.json";

        private readonly LiftrAzureFactory _azFactory;
        private readonly HostingOptions _hostingOptions;
        private readonly HostingEnvironmentOptions _envOptions;
        private readonly Serilog.ILogger _logger;

        public ThanosAssetRegistryManager(
            LiftrAzureFactory azFactory,
            HostingOptions hostingOptions,
            HostingEnvironmentOptions envOptions,
            Serilog.ILogger logger)
        {
            _azFactory = azFactory ?? throw new ArgumentNullException(nameof(azFactory));
            _hostingOptions = hostingOptions ?? throw new ArgumentNullException(nameof(hostingOptions));
            _envOptions = envOptions ?? throw new ArgumentNullException(nameof(envOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task UpdateAKSThanosEndpointsAsync(
            string aksResourceId,
            Region aksRegion,
            string thanosEndpoint1,
            string thanosEndpoint2)
        {
            if (string.IsNullOrEmpty(aksResourceId))
            {
                throw new ArgumentNullException(nameof(aksResourceId));
            }

            if (aksRegion == null)
            {
                throw new ArgumentNullException(nameof(aksRegion));
            }

            var globalStorageAccount = await GetGlobalStorageAccountAsync();
#pragma warning disable CS0618 // Type or member is obsolete
            var connectionString = await globalStorageAccount.GetPrimaryConnectionStringAsync();
#pragma warning restore CS0618 // Type or member is obsolete

            var thanosAsset = await GetExistingThanosAssetRegistryAsync(connectionString);

            if (thanosAsset.Regions == null)
            {
                thanosAsset.Regions = new List<RegionInfo>();
            }

            var currentRegion = thanosAsset.Regions.FirstOrDefault(r => r.RegionName.OrdinalEquals(aksRegion.Name));
            if (currentRegion == null)
            {
                currentRegion = new RegionInfo()
                {
                    RegionName = aksRegion.Name,
                };

                thanosAsset.Regions.Add(currentRegion);
            }

            var currentCluster = currentRegion.Clusters.FirstOrDefault(c => c.AKSResourceId.OrdinalEquals(aksResourceId));
            if (currentCluster == null)
            {
                currentCluster = new AKSClusterInfo()
                {
                    AKSResourceId = aksResourceId,
                };

                currentRegion.Clusters.Add(currentCluster);
            }

            currentCluster.Endpoints = new List<string>() { thanosEndpoint1, thanosEndpoint2 };

            thanosAsset = await RemoveStaleInformationAsync(thanosAsset);

            await SaveThanosAssetRegistryAsync(connectionString, thanosAsset);
        }

        private async Task<ThanosAsset> RemoveStaleInformationAsync(ThanosAsset thanosAsset)
        {
            var result = new ThanosAsset()
            {
                PartnerName = thanosAsset.PartnerName,
                EnvironmentName = thanosAsset.EnvironmentName,
            };

            foreach (var region in thanosAsset.Regions)
            {
                var newRegionInfo = new RegionInfo()
                {
                    RegionName = region.RegionName,
                };

                foreach (var cluster in region.Clusters)
                {
                    var aksId = new Liftr.Contracts.ResourceId(cluster.AKSResourceId);
                    var liftrAzure = _azFactory.GenerateLiftrAzure(aksId.SubscriptionId);
                    var aks = await liftrAzure.GetAksClusterAsync(cluster.AKSResourceId);
                    if (aks != null)
                    {
                        var newClusterInfo = new AKSClusterInfo()
                        {
                            AKSResourceId = cluster.AKSResourceId,
                            Endpoints = cluster.Endpoints,
                        };

                        newRegionInfo.Clusters.Add(newClusterInfo);
                    }
                }

                if (newRegionInfo.Clusters.Count > 0)
                {
                    result.Regions.Add(newRegionInfo);
                }
            }

            return result;
        }

        private async Task<IStorageAccount> GetGlobalStorageAccountAsync()
        {
            var globalNamingContext = new NamingContext(_hostingOptions.PartnerName, _hostingOptions.ShortPartnerName, _envOptions.EnvironmentName, _envOptions.Global.Location);
            var globalRGName = globalNamingContext.ResourceGroupName(_envOptions.Global.BaseName);
            var globalStorageAccountName = globalNamingContext.StorageAccountName(_envOptions.Global.BaseName);
            var liftrAzure = _azFactory.GenerateLiftrAzure(_envOptions.AzureSubscription.ToString());

            var globalStorageAccount = await liftrAzure.GetStorageAccountAsync(globalRGName, globalStorageAccountName);
            if (globalStorageAccount == null)
            {
                throw new InvalidOperationException("Cannot find global storage account.");
            }

            return globalStorageAccount;
        }

        private async Task<ThanosAsset> GetExistingThanosAssetRegistryAsync(string connectionString)
        {
            var blob = await GetAssetBlobAsync(connectionString, createContainerIfNotExist: true);
            bool exist = await blob.ExistsAsync();
            if (exist)
            {
                using (var memoryStream = new MemoryStream())
                {
                    string text;
                    await blob.DownloadToAsync(memoryStream);
                    text = Encoding.UTF8.GetString(memoryStream.ToArray());
                    return text.FromJson<ThanosAsset>();
                }
            }

            return new ThanosAsset()
            {
                PartnerName = _hostingOptions.PartnerName,
                EnvironmentName = _envOptions.EnvironmentName,
            };
        }

        private static async Task SaveThanosAssetRegistryAsync(string connectionString, ThanosAsset thanosAsset)
        {
            var blob = await GetAssetBlobAsync(connectionString, createContainerIfNotExist: false);
            var serilizedContent = thanosAsset.ToJson(indented: true);
            byte[] byteArray = Encoding.UTF8.GetBytes(serilizedContent);
            using MemoryStream stream = new MemoryStream(byteArray);
            await blob.UploadAsync(stream, overwrite: true);
        }

        private static async Task<BlobClient> GetAssetBlobAsync(string connectionString, bool createContainerIfNotExist)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            var blobContainer = blobServiceClient.GetBlobContainerClient(BlobContainerName);

            if (createContainerIfNotExist)
            {
                await blobContainer.CreateIfNotExistsAsync();
            }

            return blobContainer.GetBlobClient(BlobName);
        }
    }
}
