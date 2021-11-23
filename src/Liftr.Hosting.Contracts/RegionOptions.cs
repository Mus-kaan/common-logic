//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Hosting.Contracts
{
    public class RegionOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        /// <summary>
        /// Kubenetes version. This one will overwrite the global setting of <seealso cref="AKSInfo.KubernetesVersion"/>.
        /// </summary>
        public string KubernetesVersion { get; set; }

        public bool ZoneRedundant { get; set; }

        public bool? CreateDBWithZoneRedundancy { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public bool SupportAvailabilityZone { get; set; } = true;

        public void CheckValid(bool isAKS)
        {
            if (string.IsNullOrEmpty(DataBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(DataBaseName)} cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ComputeBaseName))
            {
                throw new InvalidHostingOptionException($"{nameof(ComputeBaseName)} cannot be null or empty.");
            }

            if (isAKS && SupportAvailabilityZone)
            {
                AvailabilityZoneRegionLookup.HasSupportAKS(Location);
            }
        }
    }
}
