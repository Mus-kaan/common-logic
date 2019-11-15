//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Provisioning;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class InfraV2RegionalComputeTests
    {
        private readonly ITestOutputHelper _output;

        public InfraV2RegionalComputeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task VerifyRegionalComputeResourceCreationAsync()
        {
            var context = new NamingContext("IncrediBuild", "ib", EnvironmentType.Test, Region.USEast);
            TestCommon.AddCommonTags(context.Tags);

            var baseName = SdkContext.RandomResourceName("e", 6);
            var rgName = context.ResourceGroupName(baseName);

            var model = JsonConvert.DeserializeObject<ComputeTestModel>(File.ReadAllText("ComputeTestModel.json"));
            model.Options.ComputeBaseName = baseName;

            using (var scope = new TestResourceGroupScope(rgName))
            {
                try
                {
                    var infra = new InftrastructureV2(scope.AzFactory, scope.Logger);
                    var client = scope.Client;

                    // This will take a long time. Be patient.
                    (var kv, var msi, var aks) = await infra.CreateOrUpdateRegionalComputeRGAsync(
                        context,
                        model.Options,
                        model.AKS,
                        TestCredentials.KeyVaultClient,
                        model.GenevaCert,
                        model.SSLCert,
                        model.FirstPartyCert);

                    // Check resource group.
                    {
                        var rg = await client.GetResourceGroupAsync(scope.ResourceGroupName);
                        Assert.Equal(scope.ResourceGroupName, rg.Name);
                        TestCommon.CheckCommonTags(rg.Inner.Tags);
                    }

                    // Same deployment will not throw exception.
                    await infra.CreateOrUpdateRegionalComputeRGAsync(
                        context,
                        model.Options,
                        model.AKS,
                        TestCredentials.KeyVaultClient,
                        model.GenevaCert,
                        model.SSLCert,
                        model.FirstPartyCert);
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, $"{nameof(VerifyRegionalComputeResourceCreationAsync)} failed.");
                    throw;
                }
            }
        }
    }
}
