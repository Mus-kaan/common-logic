//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Liftr.ImageBuilder.Tests
{
    public class ImageBuilderOptionsTests
    {
        [Fact]
        public void ImageBuilderOptionsJsonTest()
        {
            var options = new BuilderOptions()
            {
                SubscriptionId = new Guid(TestCredentials.SubscriptionId),
                Location = TestCommon.Location,
                ResourceGroupName = "testRGName",
                ImageGalleryName = "testsig",
                ImageReplicationRegions = new List<Region>()
                    {
                        Region.USEast,
                    },
            };

            options.CheckValid();

            var originalText = options.ToJson();
            var recovered = originalText.FromJson<BuilderOptions>();

            recovered.CheckValid();

            var recoveredText = recovered.ToJson();

            Assert.Equal(originalText, recoveredText);
        }
    }
}
