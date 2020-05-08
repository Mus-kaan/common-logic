//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.ImageBuilder
{
    public enum TenantType
    {
        MS,
        AME,
    }

    public class BuilderOptions
    {
        public TenantType Tenant { get; set; } = TenantType.AME;

        public bool KeepAzureVMImageBuilderLogs { get; set; } = false;

        public bool ExportVHDToStorage { get; set; } = true;

        public Guid SubscriptionId { get; set; }

        public string ExecutorSPNObjectId { get; set; }

        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string ResourceGroupName { get; set; }

        public string ImageGalleryName { get; set; }

        public string PackerVMSize { get; set; } = "Standard_D2s_v3";

        public int ImageVersionRetentionTimeInDays { get; set; } = 15;

        [JsonProperty(ItemConverterType = typeof(RegionConverter))]
        public IEnumerable<Region> ImageReplicationRegions { get; set; }

        /// <summary>
        /// The number of replicas of the Image Version to be created per region. This property is updatable after creation.
        /// </summary>
        public int RegionalReplicaCount { get; set; } = 1;

        public ContentStoreOptions ContentStoreOptions { get; set; } = new ContentStoreOptions();

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(ResourceGroupName))
            {
                throw new InvalidOperationException($"{nameof(ResourceGroupName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ImageGalleryName))
            {
                throw new InvalidOperationException($"{nameof(ImageGalleryName)} cannot be null or empty.");
            }

            if (Location == null)
            {
                throw new InvalidOperationException($"{nameof(Location)} cannot be null.");
            }

            if (ImageVersionRetentionTimeInDays < 1)
            {
                throw new InvalidOperationException($"{nameof(ImageVersionRetentionTimeInDays)} must be greater than 1.");
            }
        }
    }
}
