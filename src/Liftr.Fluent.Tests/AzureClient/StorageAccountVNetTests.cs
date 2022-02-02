//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class StorageAccountVNetTests
    {
        private readonly ITestOutputHelper _output;

        public StorageAccountVNetTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task CanCreateWithVNetAsync()
        {
            using (var scope = new TestResourceGroupScope("st-vnet-", _output))
            {
                try
                {
                    var logger = scope.Logger;
                    var azFactory = scope.AzFactory;
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("st", 15);

                    var vnet = await az.GetOrCreateVNetAsync(TestCommon.Location, scope.ResourceGroupName, SdkContext.RandomResourceName("vnet", 9), TestCommon.Tags);
                    var subnet = vnet.Subnets.FirstOrDefault().Value;

                    var st = await az.GetOrCreateStorageAccountAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags, subnet?.Inner?.Id);
                    Assert.Equal(DefaultAction.Deny, st.Inner.NetworkRuleSet.DefaultAction);

                    st = await st.TurnOffVNetAsync(logger);
                    Assert.Equal(DefaultAction.Allow, st.Inner.NetworkRuleSet.DefaultAction);
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
