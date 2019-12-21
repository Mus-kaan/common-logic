//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2
{
    public class EV2ImageBuilderOptions : BaseEV2Options
    {
        public IEnumerable<ImageOptions> Images { get; set; }

        public void CheckValid()
        {
            if (Images == null || !Images.Any())
            {
                var ex = new InvalidOperationException($"Please make sure {nameof(Images)} is not empty.");
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
        public EV2RunnerInfomation RunnerInformation { get; set; }

        public string ImageName { get; set; }

        public SourceImageType SourceImage { get; set; }

        public string ConfigurationPath { get; set; }

        public void CheckValid()
        {
            if (RunnerInformation == null)
            {
                var ex = new InvalidOperationException($"Please make sure {nameof(RunnerInformation)} is not empty.");
                throw ex;
            }

            if (string.IsNullOrEmpty(ImageName))
            {
                var ex = new InvalidOperationException($"Please make sure {nameof(ImageName)} is not empty.");
                throw ex;
            }

            if (string.IsNullOrEmpty(ConfigurationPath))
            {
                var ex = new InvalidOperationException($"Please make sure {nameof(ConfigurationPath)} is not empty.");
                throw ex;
            }

            if (RunnerInformation == null)
            {
                var ex = new InvalidOperationException($"Please make sure '{nameof(RunnerInformation)}' is not empty.");
                throw ex;
            }

            RunnerInformation.CheckValid();
        }
    }
}
