//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Liftr.SimpleDeploy
{
    public abstract class BaseResourceOptions
    {
        public EnvironmentType Environment { get; set; }

        public string LocationStr { get; set; }

        [JsonIgnore]
        public Region Location => Region.Create(LocationStr);
    }
}
