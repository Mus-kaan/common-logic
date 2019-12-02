//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class AKSInfo
    {
        public string AKSRootUserName { get; set; }

        public string AKSSSHPublicKey { get; set; }

        public string AKSSPNClientId { get; set; }

        public string AKSSPNObjectId { get; set; }

        public string AKSSPNClientSecretName { get; set; }

        public int AKSMachineCount { get; set; }

        public string AKSMachineTypeStr { get; set; }

        [JsonIgnore]
        public ContainerServiceVMSizeTypes AKSMachineType => ContainerServiceVMSizeTypes.Parse(AKSMachineTypeStr);

        public void CheckValues()
        {
            if (string.IsNullOrEmpty(AKSRootUserName))
            {
                throw new InvalidOperationException($"{nameof(AKSRootUserName)} is not valid.");
            }

            if (string.IsNullOrEmpty(AKSSSHPublicKey))
            {
                throw new InvalidOperationException($"{nameof(AKSSSHPublicKey)} is not valid.");
            }

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

            if (string.IsNullOrEmpty(AKSMachineTypeStr))
            {
                throw new InvalidOperationException($"{nameof(AKSMachineTypeStr)} is not valid.");
            }

            if (AKSMachineType == null)
            {
                throw new InvalidOperationException($"{nameof(AKSMachineType)} is not valid.");
            }
        }
    }
}
