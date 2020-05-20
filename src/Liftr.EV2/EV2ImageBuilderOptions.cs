//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2
{
    public class EV2ImageBuilderOptions : BaseEV2Options
    {
        /// <summary>
        /// The list of the generating VM images.
        /// </summary>
        public IEnumerable<ImageOptions> Images { get; set; }

        public void CheckValid()
        {
            if (Images == null || !Images.Any())
            {
                var ex = new InvalidImageBuilderEV2OptionsException($"Please make sure '{nameof(Images)}' section is not empty.");
                throw ex;
            }

            foreach (var images in Images)
            {
                images.CheckValid();
            }
        }
    }

    public class ImageOptions
    {
        /// <summary>
        /// Name of the image, this will be used as the Shared Image Gallery image name.
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// Source base image. The generating VM image will be customized from this source image.
        /// </summary>
        public SourceImageType SourceImage { get; set; }

        /// <summary>
        /// Define the image baking step. It is highly recommanded to bake the VM image in Public Azure. AIB may not be availabe in other clouds.
        /// </summary>
        public EnvironmentOptions Bake { get; set; }

        /// <summary>
        /// Define the image distribution steps. The image be can imported to Fairfax, Mooncake, USNat and USSec.
        /// </summary>
        public IEnumerable<EnvironmentOptions> Distribute { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(ImageName))
            {
                var ex = new InvalidImageBuilderEV2OptionsException($"Please make sure '{nameof(ImageName)}' is not empty.");
                throw ex;
            }

            if (Bake == null)
            {
                var ex = new InvalidImageBuilderEV2OptionsException($"Please make sure '{nameof(Bake)}' section is not empty.");
                throw ex;
            }

            Bake.CheckValid();

            if (Distribute == null || !Distribute.Any())
            {
                return;
            }

            foreach (var dist in Distribute)
            {
                dist.CheckValid();
            }
        }
    }

    public class EnvironmentOptions
    {
        /// <summary>
        /// Cloud type name.
        /// </summary>
        public CloudType Cloud { get; set; } = CloudType.Public;

        /// <summary>
        /// Path to the configuration file. Each Cloud need its own configuration file.
        /// </summary>
        public string ConfigurationPath { get; set; }

        /// <summary>
        /// Define the Managed Identity used by the EV2 shell extension. https://ev2docs.azure.net/features/extensibility/shell/intro.html#managed-identity
        /// </summary>
        public EV2RunnerInfomation RunnerInformation { get; set; }

        public void CheckValid()
        {
            if (string.IsNullOrEmpty(ConfigurationPath))
            {
                var ex = new InvalidImageBuilderEV2OptionsException($"Please make sure '{nameof(ConfigurationPath)}' is not empty.");
                throw ex;
            }

            if (RunnerInformation == null)
            {
                var ex = new InvalidImageBuilderEV2OptionsException($"Please make sure '{nameof(RunnerInformation)}' section is not empty.");
                throw ex;
            }

            RunnerInformation.CheckValid();
        }
    }
}
