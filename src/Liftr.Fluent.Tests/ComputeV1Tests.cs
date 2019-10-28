//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning.MultiTenant;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public class ComputeV1Tests
    {
        private readonly ITestOutputHelper _output;

        public ComputeV1Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "This need private link service subscription white listing from NRP team.")]
        public async Task CanCreateILBAsync()
        {
            var baseName = "comp";
            var namingContext = new NamingContext("FakePartner", SdkContext.RandomResourceName("paas", 10), EnvironmentType.Test, Region.USWestCentral);
            using (var scope = new TestResourceGroupScope(baseName, namingContext, _output))
            {
                try
                {
                    var c = new ComputeV1(scope.Client, scope.Logger);
                    await c.CreateServiceClusterAsync(baseName, namingContext, VirtualMachineScaleSetSkuTypes.StandardA0, "vmuser123", Guid.NewGuid().ToString());
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }
    }
}
