﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Provisioning;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests.AzureClient
{
    public sealed class DeploymentTests
    {
        private readonly ITestOutputHelper _output;

        public DeploymentTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task DeploymentFailureWillThrowAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-deployment-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);

                var template = TemplateHelper.GeneratePrivateLinkServiceTemplate(TestCommon.Location, "plsName", "net-rid", "frontend-rid");

                await Assert.ThrowsAsync<ARMDeploymentFailureException>(async () =>
                {
                    await client.CreateDeploymentAsync(TestCommon.Location, rg.Name, template);
                });
            }
        }
    }
}