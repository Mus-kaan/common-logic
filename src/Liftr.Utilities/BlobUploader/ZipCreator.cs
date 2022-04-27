//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Serilog;
using System;
using System.IO;
using System.IO.Compression;

namespace Microsoft.Liftr.Utilities.BlobUploader
{
    public static class ZipCreator
    {
        /// <summary>
        /// Compress the given directory to a file at targetFilePath
        /// </summary>
        /// <param name="sourceDirectory">Source directory</param>
        /// <param name="targetFilePath">TargetFilepath</param>
        /// <param name="logger">logger</param>
        /// <exception cref="FileNotFoundException">If sourceDirectory does not exist</exception>
        /// <exception cref="ArgumentException">If the targetFilePath already exists</exception>
        /// <exception cref="ArgumentNullException">If the targetFilePath is empty</exception>
        public static void CompressDirectory(string sourceDirectory, string targetFilePath, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!Directory.Exists(sourceDirectory))
            {
                logger.Error($"[{BlobUploaderConstants.BlobUploadTag}] {sourceDirectory} does not exist");
                throw new FileNotFoundException($"{sourceDirectory} does not exist");
            }

            if (string.IsNullOrWhiteSpace(targetFilePath))
            {
                logger.Error($"[{BlobUploaderConstants.BlobUploadTag}] {targetFilePath} is null or empty");
                throw new ArgumentNullException($"{targetFilePath} is null or empty");
            }

            if (File.Exists(targetFilePath))
            {
                logger.Error($"[{BlobUploaderConstants.BlobUploadTag}] {targetFilePath} already exists");
                throw new ArgumentException($" {targetFilePath} already exists");
            }

            ZipFile.CreateFromDirectory(sourceDirectory, targetFilePath);
            logger.Information($"[{BlobUploaderConstants.BlobUploadTag}] File compressed to targetFilePath successfully");
        }
    }
}
