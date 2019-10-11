//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class ComputeResourceOptions : BaseResourceOptions
    {
        public string GlobalBaseName { get; set; }

        public string DataBaseName { get; set; }

        public string ComputeBaseName { get; set; }

        public string DBConnectionStringSecretName { get; set; }

        public string HostName { get; set; }

        public string GlobalLocationStr { get; set; }

        [JsonIgnore]
        public Region GlobalLocation => Region.Create(GlobalLocationStr);

        public IEnumerable<string> DataPlaneSubscriptions { get; set; }
    }
}
