//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public interface IContentStore
    {
        Task<Uri> UploadBuildArtifactsAndGenerateReadSASAsync(string filePath);

        Task<Uri> CopySourceSBIAsync(string sbiVhdVersion, string sourceVHDSASToken);

        Task<Uri> CopyGeneratedVHDAsync(string sourceVHDSASToken, string imageName, string imageVersion);

        Task<int> CleanUpOldArtifactsAsync();

        Task<int> CleanUpExportingVHDsAsync();
    }
}
