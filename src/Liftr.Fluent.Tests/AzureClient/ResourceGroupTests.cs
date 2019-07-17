﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.Fluent.Tests
{
    public sealed class ResourceGroupTests
    {
        private readonly ITestOutputHelper _output;

        public ResourceGroupTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CanCreateAndDeleteGroupAsync()
        {
            using (var scope = new TestResourceGroupScope("unittest-rg-", _output))
            {
                var client = scope.Client;
                var rg = await client.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                var retrieved = await client.GetResourceGroupAsync(scope.ResourceGroupName);

                TestCommon.CheckCommonTags(retrieved.Inner.Tags);

                await client.DeleteResourceGroupAsync(scope.ResourceGroupName);

                // It is deleted.
                Assert.Null(await client.GetResourceGroupAsync(scope.ResourceGroupName));
            }
        }
    }
}
