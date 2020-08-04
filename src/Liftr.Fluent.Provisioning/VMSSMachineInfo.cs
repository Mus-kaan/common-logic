//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class VMSSMachineInfo
    {
        public int MachineCount { get; set; } = 3;

        public string VMSize { get; set; } = "Standard_F2s_v2";

        public string GalleryImageVersionId { get; set; }

        public void CheckValues()
        {
            if (MachineCount < 3)
            {
                throw new InvalidOperationException($"{nameof(MachineCount)} should >= 3.");
            }

            VMSSSkuHelper.ParseSkuString(VMSize);

            var rid = new ResourceId(GalleryImageVersionId);
        }
    }
}
