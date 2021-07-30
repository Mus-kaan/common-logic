//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Liftr.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Hosting.Contracts;

namespace Microsoft.Liftr.SimpleDeploy
{
    public class SimpleDeployConfigurations
    {
        public LiftrAzureFactory LiftrAzureFactory { get; set; }

        public KeyVaultClient KeyVaultClient { get; set; }

        public RunnerCommandOptions RunnerCommandOptions { get; set; }

        public HostingOptions HostingOptions { get; set; }

        public HostingEnvironmentOptions EnvironmentOptions { get; set; }

        public Serilog.ILogger Logger { get; set; }

        public NamingContext GlobalNamingContext { get; set; }

        public NamingContext RegionalNamingContext { get; set; }
    }
}
