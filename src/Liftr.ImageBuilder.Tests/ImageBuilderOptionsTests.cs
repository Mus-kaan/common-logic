//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class ImageBuilderOptionsTests
    {
        [Fact]
        public void ImageBuilderOptionsJsonTest()
        {
            var original = new ImageBuilderOptions()
            {
                ResourceGroupName = "testRGName",
                GalleryName = "galleryName",
                ImageDefinitionName = "imgDef",
                StorageAccountName = "storageAccountName",
                Location = Region.AsiaEast,
                ImageVersionTTLInDays = 10,
            };

            original.Tags = new Dictionary<string, string>()
            {
                ["asdasd"] = "asdasd",
                ["sdfsdf"] = "fdgdf",
            };

            var originalText = original.ToJson();
            var recovered = originalText.FromJson<ImageBuilderOptions>();
            var recoveredText = recovered.ToJson();

            Assert.Equal(originalText, recoveredText);
        }
    }
}
