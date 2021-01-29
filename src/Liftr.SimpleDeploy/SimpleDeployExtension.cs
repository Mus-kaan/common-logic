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

        /// <summary>
        /// Call back after the regional data resources are created. This assume that data and compute are in the SAME region.
        /// </summary>
        public static Func<RegionalDataCallbackParameters, Task> AfterProvisionRegionalDataResourcesAsync { get; set; }

        public static Func<AKSCallbackParameters, Task> AfterProvisionRegionalAKSResourcesAsync { get; set; }

        public static Func<VMSSCallbackParameters, Task> AfterProvisionRegionalVMSSResourcesAsync { get; set; }
    }
}
