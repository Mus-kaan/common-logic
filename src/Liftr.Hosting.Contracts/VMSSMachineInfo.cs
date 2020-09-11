//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Microsoft.Liftr.Fluent.Contracts;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class VMSSMachineInfo
    {
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
    }
}
