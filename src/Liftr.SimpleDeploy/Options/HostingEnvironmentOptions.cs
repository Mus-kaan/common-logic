//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class HostingEnvironmentOptions
    {
        public EnvironmentType EnvironmentName { get; set; }

        public Guid AzureSubscription { get; set; }

        public GlobalOptions Global { get; set; }

        public IEnumerable<RegionOptions> Regions { get; set; }
    }

    public class GlobalOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string BaseName { get; set; }
    }

    public class RegionOptions
    {
        [JsonConverter(typeof(RegionConverter))]
        public Region Location { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public string HostName { get; set; }

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }
    }
}
