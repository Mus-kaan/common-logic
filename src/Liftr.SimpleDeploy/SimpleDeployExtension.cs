//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public static class SimpleDeployExtension
    {
        public static Func<SimpleDeployConfigurations, Task> AfterRunAsync { get; set; }

        public static Func<GlobalCallbackParameters, Task> AfterProvisionGlobalResourcesAsync { get; set; }

        public static Func<RegionalDataCallbackParameters, Task> AfterProvisionRegionalDataResourcesAsync { get; set; }

        public static Func<VMSSCallbackParameters, Task> AfterProvisionRegionalVMSSResourcesAsync { get; set; }
    }
}
