//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class AKSInfo
    {
        public int AKSMachineCount { get; set; } = 3;

        [JsonConverter(typeof(ContainerServiceVMSizeTypesConverter))]
        public ContainerServiceVMSizeTypes AKSMachineType { get; set; }

        public void CheckValues()
        {
            if (AKSMachineCount < 3)
            {
                throw new InvalidOperationException($"{nameof(AKSMachineCount)} should >= 3.");
            }

            if (AKSMachineType == null)
            {
                throw new InvalidOperationException($"{nameof(AKSMachineType)} is not valid.");
            }
        }
    }
}
