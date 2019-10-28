//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InfraV2GlobalRGTests
    {
        private readonly ITestOutputHelper _output;

        public InfraV2GlobalRGTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyGlobalResourceGroupAsync()
        {
            var shortPartnerName = "v2";
            var context = new NamingContext("Infrav2Partner", shortPartnerName, EnvironmentType.Test, Region.USWest);
            TestCommon.AddCommonTags(context.Tags);

            var globalCoreName = SdkContext.RandomResourceName("v", 3);
            var globalRGName = context.ResourceGroupName(globalCoreName);

            using (var globalScope = new TestResourceGroupScope(globalRGName))
            {
                var clientFactory = new LiftrAzureFactory(globalScope.Logger, TestCredentials.SubscriptionId, TestCredentials.GetAzureCredentials);
                var client = clientFactory.GenerateLiftrAzure();
                var infra = new InftrastructureV2(clientFactory, globalScope.Logger);

                // This will take a long time. Be patient. About 6 minutes.
                var kv = await infra.CreateOrUpdateGlobalRGAsync(globalCoreName, context, TestCredentials.ClientId);

                // Check global resource group.
                {
                    var rg = await client.GetResourceGroupAsync(globalScope.ResourceGroupName);
                    Assert.Equal(globalScope.ResourceGroupName, rg.Name);
                    TestCommon.CheckCommonTags(rg.Inner.Tags);
                }

                // Same deployment will not throw exception.
                await infra.CreateOrUpdateGlobalRGAsync(globalCoreName, context, TestCredentials.ClientId);
            }
        }
    }
}
