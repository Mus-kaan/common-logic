//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.Fluent;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.SimpleDeploy
{
    public static class SimpleDeployExtension
    {
        public static Func<LiftrAzureFactory, KeyVaultClient, RunnerCommandOptions, EnvironmentOptions, Serilog.ILogger, Task> AfterRunAsync { get; set; }
    }
}
