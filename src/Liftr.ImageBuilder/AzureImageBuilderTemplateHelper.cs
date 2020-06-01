//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Liftr.ImageBuilder
{
    public class AzureImageBuilderTemplateHelper
    {
        private const string c_ARTIFACT_URI_PLACEHOLDER = "ARTIFACT_URI_PLACEHOLDER";
        private const string c_USER_ASSIGNED_MI_ID_PLACEHOLDER = "USER_ASSIGNED_MI_ID_PLACEHOLDER";
        private const string c_REPLICATION_REGIONS_PLACEHOLDER = "\"REPLICATION_REGIONS_PLACEHOLDER\"";
        private const string c_linux_aib_base_template = "Microsoft.Liftr.ImageBuilder.aib.template.linux.json";
        private const string c_windows_platform_aib_base_template = "Microsoft.Liftr.ImageBuilder.aib.template.windows.json";

        private readonly BuilderOptions _options;
        private readonly ITimeSource _timeSource;

        public AzureImageBuilderTemplateHelper(
            BuilderOptions options,
            ITimeSource timeSource)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public string GenerateLinuxSBITemplate(
            Region location,
            string imageTemplateName,
            string imageName,
            string imageVersion,
            string artifactLinkWithSAS,
            string msiId,
            string srcImgVersionId,
            IDictionary<string, string> tags = null,
            bool formatJson = true)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            Dictionary<string, string> srcImg = new Dictionary<string, string>()
            {
                ["type"] = "SharedImageVersion",
                ["imageVersionId"] = srcImgVersionId,
            };

            return GenerateImageTemplate(
                c_linux_aib_base_template,
                location,
                imageTemplateName,
                imageName,
                imageVersion,
                artifactLinkWithSAS,
                msiId,
                formatJson,
                srcImg,
                tags);
        }

        public string GenerateLinuxPlatformImageTemplate(
            Region location,
            string imageTemplateName,
            string imageName,
            string imageVersion,
            string artifactLinkWithSAS,
            string msiId,
            PlatformImageIdentifier sourceImage,
            IDictionary<string, string> tags = null,
            bool formatJson = true)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (sourceImage == null)
            {
                throw new ArgumentNullException(nameof(sourceImage));
            }

            Dictionary<string, string> srcImg = new Dictionary<string, string>()
            {
                ["type"] = "PlatformImage",
                ["publisher"] = sourceImage.Publisher,
                ["offer"] = sourceImage.Offer,
                ["sku"] = sourceImage.Sku,
                ["version"] = sourceImage.Version,
            };

            return GenerateImageTemplate(
                c_linux_aib_base_template,
                location,
                imageTemplateName,
                imageName,
                imageVersion,
                artifactLinkWithSAS,
                msiId,
                formatJson,
                srcImg,
                tags);
        }

        public string GenerateWinodwsPlatformImageTemplate(
            Region location,
            string imageTemplateName,
            string imageName,
            string imageVersion,
            string artifactLinkWithSAS,
            string msiId,
            PlatformImageIdentifier sourceImage,
            IDictionary<string, string> tags = null,
            bool formatJson = true)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (sourceImage == null)
            {
                throw new ArgumentNullException(nameof(sourceImage));
            }

            Dictionary<string, string> srcImg = new Dictionary<string, string>()
            {
                ["type"] = "PlatformImage",
                ["publisher"] = sourceImage.Publisher,
                ["offer"] = sourceImage.Offer,
                ["sku"] = sourceImage.Sku,
                ["version"] = sourceImage.Version,
            };

            return GenerateImageTemplate(
                c_windows_platform_aib_base_template,
                location,
                imageTemplateName,
                imageName,
                imageVersion,
                artifactLinkWithSAS,
                msiId,
                formatJson,
                source: srcImg,
                tags: tags);
        }

        private string GenerateImageTemplate(
            string templateContentFileName,
            Region location,
            string imageTemplateName,
            string imageName,
            string imageVersion,
            string artifactUrlWithSAS,
            string msiId,
            bool formatJson,
            IDictionary<string, string> source,
            IDictionary<string, string> tags)
        {
            var templateContent = EmbeddedContentReader.GetContent(Assembly.GetExecutingAssembly(), templateContentFileName);
            templateContent = templateContent.Replace(c_ARTIFACT_URI_PLACEHOLDER, artifactUrlWithSAS, StringComparison.OrdinalIgnoreCase);
            templateContent = templateContent.Replace(c_USER_ASSIGNED_MI_ID_PLACEHOLDER, msiId, StringComparison.OrdinalIgnoreCase);

            var regions = string.Join(", ", _options.ImageReplicationRegions.Select(r => $"\"{r.Name}\""));
            templateContent = templateContent.Replace(c_REPLICATION_REGIONS_PLACEHOLDER, regions, StringComparison.OrdinalIgnoreCase);

            dynamic templateObj = JObject.Parse(templateContent);

            var resourceObject = templateObj.resources[0];

            var galleryImageResourceId = $"/subscriptions/{_options.SubscriptionId}/resourceGroups/{_options.ResourceGroupName}/providers/Microsoft.Compute/galleries/{_options.ImageGalleryName}/images/{imageName}/versions/{imageVersion}";

            resourceObject.name = imageTemplateName;
            resourceObject.location = location.Name;
            resourceObject.properties.vmProfile.vmSize = _options.PackerVMSize;
            resourceObject.properties.source = source.ToJObject();

            var distributeObject = resourceObject.properties.distribute[1];
            distributeObject.galleryImageId = galleryImageResourceId;

            var artifactTags = distributeObject.artifactTags;
            artifactTags["TemplateCreationTime"] = _timeSource.UtcNow.ToZuluString();
            artifactTags[NamingContext.c_createdAtTagName] = _timeSource.UtcNow.ToZuluString();

            foreach (var kvp in source)
            {
                artifactTags["src_" + kvp.Key] = kvp.Value;
            }

            if (tags != null)
            {
                foreach (var kvp in tags)
                {
                    artifactTags[kvp.Key] = kvp.Value;
                }
            }

            return JsonConvert.SerializeObject(templateObj, formatJson ? Formatting.Indented : Formatting.None);
        }
    }
}
