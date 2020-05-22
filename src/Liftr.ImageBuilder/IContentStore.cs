//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public interface IContentStore
    {
        Task<Uri> UploadBuildArtifactsToSupportingStorageAsync(string filePath);

        Task<Uri> CopySourceSBIAsync(string sbiVhdVersion, Uri sourceVHDSASToken);

        Task<Uri> CopyVHDToExportAsync(
            Uri sourceVHDSASToken,
            string imageName,
            string imageVersion,
            SourceImageType sourceImageType,
            IReadOnlyDictionary<string, string> tags,
            string createdAt,
            string deleteAfter);

        Task<(Uri, VHDMeta)> CopyVHDToImportAsync(Uri sourceVHDSASToken, Uri sourceVHDMetaSASToken, string imageName, string imageVersion);

        Task<int> CleanUpOldArtifactsAsync();

        Task<int> CleanUpExportingVHDsAsync();

        Task<int> CleanUpImportingVHDsAsync();
    }
}
