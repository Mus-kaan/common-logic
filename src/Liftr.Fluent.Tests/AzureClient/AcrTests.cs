//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class AcrTests
    {
        private readonly ITestOutputHelper _output;

        public AcrTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CreateAcrAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-acr-", _output))
            {
                try
                {
                    var logger = scope.Logger;
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("acr", 15);

                    var acr = await az.GetOrCreateACRAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);
                    Assert.Equal("Premium", acr.Sku.Name);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "Failed.");
                    scope.TimedOperation.FailOperation(ex.Message);
                    throw;
                }
            }
        }
    }
}
