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

        public string AKSSPNClientId { get; set; }

        public string AKSSPNObjectId { get; set; }

        public string AKSSPNClientSecretName { get; set; } = "AKSSPClientSecret";

        public void CheckValues()
        {
            if (string.IsNullOrEmpty(AKSSPNClientId))
            {
                throw new InvalidOperationException($"{nameof(AKSSPNClientId)} is not valid.");
            }

            if (string.IsNullOrEmpty(AKSSPNObjectId))
            {
                throw new InvalidOperationException($"{nameof(AKSSPNObjectId)} is not valid.");
            }

            if (string.IsNullOrEmpty(AKSSPNClientSecretName))
            {
                throw new InvalidOperationException($"{nameof(AKSSPNClientSecretName)} is not valid.");
            }

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
