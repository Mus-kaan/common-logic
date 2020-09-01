//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using System;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public static class VMSSSkuHelper
    {
        private const string c_standardPrefix = "Standard_";

        public static VirtualMachineScaleSetSkuTypes ParseSkuString(string vmSize)
        {
            if (!vmSize.OrdinalStartsWith(c_standardPrefix))
            {
                throw new InvalidOperationException("We only support 'Standard' tier. e.g. 'Standard_F2s_v2'. The input size is not supported: " + vmSize);
            }

            // https://github.com/Azure/azure-libraries-for-net/blob/master/src/ResourceManagement/Compute/VirtualMachineScaleSetSkuTypes.cs
            return new VirtualMachineScaleSetSkuTypes(vmSize, "Standard");
        }
    }
}
