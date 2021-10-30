//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ThanosAsset
    {
        public string PartnerName { get; set; }

        public EnvironmentType EnvironmentName { get; set; }

        public List<RegionInfo> Regions { get; set; } = new List<RegionInfo>();

        public List<string> ListAllThanosEndpoints()
        {
            List<string> result = new List<string>();

            if (Regions != null)
            {
                foreach (var region in Regions)
                {
                    if (region.Clusters != null)
                    {
                        foreach (var cluster in region.Clusters)
                        {
                            if (cluster?.Endpoints?.Count > 0)
                            {
                                result.AddRange(cluster.Endpoints);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }

    public class RegionInfo
    {
        public string RegionName { get; set; }

        public List<AKSClusterInfo> Clusters { get; set; } = new List<AKSClusterInfo>();
    }

    public class AKSClusterInfo
    {
        public string AKSResourceId { get; set; }

        public List<string> Endpoints { get; set; } = new List<string>();
    }
}
