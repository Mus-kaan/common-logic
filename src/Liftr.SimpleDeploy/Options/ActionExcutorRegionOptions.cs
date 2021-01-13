//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class ActionExcutorRegionOptions
    {
        public RegionOptions RegionOptions { get; set; }

        public NamingContext RegionNamingContext { get; set; }

        public ComputeRegionOptions ComputeRegionOptions { get; set; }

        public NamingContext ComputeRegionNamingContext { get; set; }

        public string AKSRGName { get; set; }

        public string AKSName { get; set; }

        public Region AKSRegion => RegionOptions.IsSeparatedDataAndComputeRegion ? ComputeRegionNamingContext.Location : RegionNamingContext.Location;

        public bool EnableAvailabilityZone => RegionOptions.IsSeparatedDataAndComputeRegion ? ComputeRegionOptions.SupportAvailabilityZone : RegionOptions.SupportAvailabilityZone;
    }
}
