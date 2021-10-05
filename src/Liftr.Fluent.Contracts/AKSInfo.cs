//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Newtonsoft.Json;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public class AKSInfo
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AKSMachineCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AKSAutoScaleMinCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AKSAutoScaleMaxCount { get; set; }

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
        public string KubernetesVersion { get; set; } = "1.21.2";

        public void CheckValues()
        {
            if (AKSMachineType == null)
            {
                throw new InvalidHostingOptionException($"{nameof(AKSMachineType)} is not valid.");
            }

            if (!AKSMachineCount.HasValue && (!AKSAutoScaleMinCount.HasValue || !AKSAutoScaleMaxCount.HasValue))
            {
                throw new InvalidHostingOptionException($"Please provide machine count information through either specify a fixed {nameof(AKSMachineCount)}, or cluster auto scaling ({nameof(AKSAutoScaleMinCount)} and {nameof(AKSAutoScaleMaxCount)}).");
            }

            if (AKSMachineCount.HasValue)
            {
                // Fixed machine count.
                if (AKSAutoScaleMinCount.HasValue || AKSAutoScaleMaxCount.HasValue)
                {
                    throw new InvalidHostingOptionException($"Cannot support both fixed machine count and auto-scale. Please choose one.");
                }

                if (AKSMachineCount < 3)
                {
                    throw new InvalidHostingOptionException($"{nameof(AKSMachineCount)} should >= 3.");
                }
            }
            else
            {
                // cluster auto scale
                if (!AKSAutoScaleMinCount.HasValue || !AKSAutoScaleMaxCount.HasValue)
                {
                    throw new InvalidHostingOptionException($"Please provide both {nameof(AKSAutoScaleMinCount)} and {nameof(AKSAutoScaleMaxCount)}.");
                }

                if (AKSAutoScaleMinCount < 2)
                {
                    throw new InvalidHostingOptionException($"{nameof(AKSAutoScaleMinCount)} should >= 2.");
                }

                if (AKSAutoScaleMaxCount > 200)
                {
                    throw new InvalidHostingOptionException($"{nameof(AKSAutoScaleMaxCount)} should <= 200.");
                }

                if (AKSAutoScaleMinCount > AKSAutoScaleMaxCount)
                {
                    throw new InvalidHostingOptionException($"{nameof(AKSAutoScaleMinCount)} should <= {nameof(AKSAutoScaleMaxCount)}.");
                }
            }
        }
    }
}
