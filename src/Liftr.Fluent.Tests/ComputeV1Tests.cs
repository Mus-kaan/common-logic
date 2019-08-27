//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning.MultiTenant;
using System;
using System.Collections.Generic;
using System.Text;
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

        [Fact]
        public async Task CanCreateILBAsync()
        {
            var logger = TestLogger.GetLogger(_output);
            var baseName = "comp";
            var namingContext = new NamingContext("FakePartner", SdkContext.RandomResourceName("paas", 10), EnvironmentType.Test, Region.USWestCentral);
            using (var scope = new TestResourceGroupScope(baseName, namingContext, _output))
            {
                try
                {
                    var c = new ComputeV1(scope.Client, logger);
                    await c.CreateServiceClusterAsync(baseName, namingContext, VirtualMachineScaleSetSkuTypes.StandardA0, "vmuser123", "Msusr@123456");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }
    }
}
