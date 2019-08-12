//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent.Models;
using System;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public static class VirtualMachineSizeExtensions
    {
        public static VirtualMachineSizeTypes ToVMSize(this string vmSize)
        {
            if (vmSize.OrdinalEquals("Standard_DS1_v2"))
            {
                return VirtualMachineSizeTypes.StandardDS1V2;
            }
            else if (vmSize.OrdinalEquals("Standard_DS2_v2"))
            {
                return VirtualMachineSizeTypes.StandardDS2V2;
            }
            else if (vmSize.OrdinalEquals("Standard_DS3_v2"))
            {
                return VirtualMachineSizeTypes.StandardDS3V2;
            }
            else if (vmSize.OrdinalEquals("Standard_DS4_v2"))
            {
                return VirtualMachineSizeTypes.StandardDS4V2;
            }

            throw new ArgumentOutOfRangeException($"Virtual machine size {vmSize} is not supported or recognized.");
        }
    }
}
