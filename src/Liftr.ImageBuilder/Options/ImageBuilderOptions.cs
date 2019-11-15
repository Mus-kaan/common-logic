//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.ImageBuilder
{
    public sealed class ImageBuilderOptions
    {
        public ImageBuilderOptions()
        {
        }

        [JsonConstructor]
        public ImageBuilderOptions(
            string resourceGroupName,
            string galleryName,
            string imageDefinitionName,
            string storageAccountName,
            string locationStr,
            IDictionary<string, string> tags,
            int imageVersionTTLInDays)
        {
            ResourceGroupName = resourceGroupName;
            GalleryName = galleryName;
            ImageDefinitionName = imageDefinitionName;
            StorageAccountName = storageAccountName;
            LocationStr = locationStr;
            Tags = tags;
            ImageVersionTTLInDays = imageVersionTTLInDays;

            CheckValid();
        }

        public string ResourceGroupName { get; set; }

        public string GalleryName { get; set; }

        public string ImageDefinitionName { get; set; }

        public string StorageAccountName { get; set; }

        [JsonIgnore]
        public Region Location
        {
            get
            {
                return Region.Create(LocationStr);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                LocationStr = value.Name;
            }
        }

        public string LocationStr { get; set; }

        public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        public int ImageVersionTTLInDays { get; set; }

        private void CheckValid()
        {
            if (string.IsNullOrEmpty(ResourceGroupName))
            {
                throw new InvalidOperationException($"{nameof(ResourceGroupName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(GalleryName))
            {
                throw new InvalidOperationException($"{nameof(GalleryName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ImageDefinitionName))
            {
                throw new InvalidOperationException($"{nameof(ImageDefinitionName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(StorageAccountName))
            {
                throw new InvalidOperationException($"{nameof(StorageAccountName)} cannot be null or empty.");
            }

            if (Location == null)
            {
                throw new InvalidOperationException($"{nameof(Location)} cannot be null.");
            }

            if (ImageVersionTTLInDays < 10)
            {
                throw new InvalidOperationException($"{nameof(ImageVersionTTLInDays)} must be greater than 10.");
            }
        }
    }
}
