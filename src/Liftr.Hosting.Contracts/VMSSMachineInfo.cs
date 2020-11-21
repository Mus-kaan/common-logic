//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;
using System;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class VMSSMachineInfo
    {
        private const string c_versions = "/versions";

        public int MachineCount { get; set; } = 3;

        public string VMSize { get; set; } = "Standard_DS2_v2";

        public string GalleryImageVersionId { get; set; }

        public bool UseSSHPassword { get; set; } = false;

        public void CheckValues()
        {
            if (MachineCount < 1)
            {
                throw new InvalidHostingOptionException($"{nameof(MachineCount)} should >= 1.");
            }

            VMSSSkuHelper.ParseSkuString(VMSize);

            var rid = new ResourceId(GalleryImageVersionId);
        }

        public void UseParsedImageVersion()
        {
            var ind = GalleryImageVersionId.IndexOf(c_versions, StringComparison.OrdinalIgnoreCase);
            var versionIdInd = ind + c_versions.Length + 1;

            var versionId = GalleryImageVersionId.Substring(versionIdInd);
            Console.WriteLine("VersionID {0}", versionId);
            if (Version.TryParse(versionId, out var parsedVersion))
            {
                versionId = parsedVersion.ToString();
            }
            else
            {
                throw new InvalidHostingOptionException($"The image version value '{GalleryImageVersionId}' is invalid. ");
            }

            GalleryImageVersionId = GalleryImageVersionId.Substring(0, versionIdInd) + versionId;
        }
    }
}
