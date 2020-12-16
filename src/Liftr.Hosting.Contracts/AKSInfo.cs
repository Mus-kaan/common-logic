//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class AKSInfo
    {
        public int AKSMachineCount { get; set; } = 3;

        [JsonConverter(typeof(ContainerServiceVMSizeTypesConverter))]
        public ContainerServiceVMSizeTypes AKSMachineType { get; set; }

        /// <summary>
        /// Kubenetes version. This need to be updated every few months.
        /// az aks get-versions --location westcentralus --output table
        /// https://aka.ms/supported-version-list
        /// https://github.com/kubernetes/kubernetes/blob/master/CHANGELOG/README.md
        /// Please check the release change to avoid breaking change:
        /// https://github.com/Azure/AKS/releases
        /// </summary>
        public string KubernetesVersion { get; set; } = "1.18.10";

        public void CheckValues()
        {
            if (AKSMachineCount < 3)
            {
                throw new InvalidHostingOptionException($"{nameof(AKSMachineCount)} should >= 3.");
            }

            if (AKSMachineType == null)
            {
                throw new InvalidHostingOptionException($"{nameof(AKSMachineType)} is not valid.");
            }
        }
    }
}
