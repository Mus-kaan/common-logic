//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent.Models;
using System;
using Xunit;

namespace Microsoft.Liftr.Fluent.Contracts.Tests
{
    public class VirtualMachineSizeTests
    {
        [Fact]
        public void VirtualMachineSizeNameValidation()
        {
            Assert.Equal(VirtualMachineSizeTypes.StandardDS1V2, "Standard_DS1_v2".ToVMSize());

            Assert.Equal(VirtualMachineSizeTypes.StandardDS2V2, "Standard_DS2_v2".ToVMSize());

            Assert.Equal(VirtualMachineSizeTypes.StandardDS3V2, "Standard_DS3_v2".ToVMSize());

            Assert.Equal(VirtualMachineSizeTypes.StandardDS4V2, "Standard_DS4_v2".ToVMSize());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                "UnrecognizedSize".ToVMSize();
            });
        }
    }
}
