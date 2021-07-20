//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.ImageBuilder;
using System;
using System.Threading.Tasks;

namespace BaseImageBuilder
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ImageBuilderExtension.BeforeSendingArtifactsToPackerAsync += BeforeSendingArtifactsToPackerAsync;

            Microsoft.Liftr.ImageBuilder.Program.Main(args);
        }

        public static async Task BeforeSendingArtifactsToPackerAsync(CallbackParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var logger = parameters.Logger;
            var tokenCredential = parameters.LiftrAzureFactory.TokenCredential;
            var extraProperties = parameters.BuilderOptions.Properties;
            var localPackerFolder = parameters.PackerFileFolder;

            logger.Information("Local packer folder is: {localPackerFolder}", localPackerFolder);

            await Task.Yield();
        }
    }
}
